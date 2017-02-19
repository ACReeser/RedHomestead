using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Rovers;
using RedHomestead.Buildings;
using RedHomestead.Simulation;
using RedHomestead.Equipment;
using RedHomestead.Interiors;
using RedHomestead.Geography;

namespace RedHomestead.Equipment
{
    public enum Equipment { Locked = -1, EmptyHand = 0, Drill, Blueprints, ChemicalSniffer, Wheelbarrow, Scanner, Wrench, Sidearm, LMG, Screwdriver}
    public enum Slot { Unequipped = 4, PrimaryTool = 5, SecondaryTool = 3, PrimaryGadget = 1, SecondaryGadget = 0, TertiaryGadget = 2 }

    public class Loadout
    {
        private Equipment[] OutdoorGadgets = new Equipment[] { Equipment.Blueprints, Equipment.ChemicalSniffer, Equipment.Locked };
        private Equipment[] IndoorGadgets = new Equipment[] { Equipment.Screwdriver, Equipment.Wheelbarrow, Equipment.Locked };

        private Dictionary<Slot, Equipment> _loadout = new Dictionary<Slot, Equipment>()
        {
            { Slot.Unequipped, Equipment.EmptyHand },
            { Slot.PrimaryTool, Equipment.Drill },
            { Slot.SecondaryTool, Equipment.Locked },
            { Slot.PrimaryGadget, Equipment.Blueprints },
            { Slot.SecondaryGadget, Equipment.ChemicalSniffer },
            { Slot.TertiaryGadget, Equipment.Locked },
        };

        public Equipment this[Slot s]
        {
            get
            {
                return _loadout[s];
            }
        }

        public Slot ActiveSlot { get; set; }
        public Equipment Equipped
        {
            get
            {
                return this[this.ActiveSlot];
            }
        }

        public Loadout()
        {
            this.ActiveSlot = Slot.Unequipped;
        }

        public bool IsConstructingExterior
        {
            get
            {
                return Equipped == Equipment.Blueprints && SurvivalTimer.Instance.UsingPackResources;
            }
        }

        public bool IsConstructingInterior
        {
            get
            {
                return Equipped == Equipment.Blueprints && SurvivalTimer.Instance.IsInHabitat;
            }
        }

        public void RefreshGadgetsBasedOnLocation()
        {
            if (SurvivalTimer.Instance.IsInHabitat)
            {
                _loadout[Slot.PrimaryGadget] = IndoorGadgets[0];
                _loadout[Slot.SecondaryGadget] = IndoorGadgets[1];
                _loadout[Slot.TertiaryGadget] = IndoorGadgets[2];
            }
            else
            {
                _loadout[Slot.PrimaryGadget] = OutdoorGadgets[0];
                _loadout[Slot.SecondaryGadget] = OutdoorGadgets[1];
                _loadout[Slot.TertiaryGadget] = OutdoorGadgets[2];
            }

            GuiBridge.Instance.BuildRadialMenu(this);
        }

        internal void PutEquipmentInSlot(Slot aSlot, Equipment e)
        {
            this._loadout[aSlot] = e;
        }
    }

    [Serializable]
    public struct EquipmentSprites
    {
        public Sprite[] Sprites;
        public Sprite Locked;

        internal Sprite FromEquipment(Equipment e)
        {
            if (e == Equipment.Locked)
                return Locked;

            return Sprites[(int)e];
        }
    }
}

/// <summary>
/// Responsible for raycasting, modes, and gameplay input
/// </summary>
public class PlayerInput : MonoBehaviour {
    public static PlayerInput Instance;

    public enum InputMode { Normal, PostIt, Sleep }

    private const float InteractionRaycastDistance = 10f;
    private const float EVAChargerPerSecond = 7.5f;
    private const int ChemicalFlowLayerIndex = 9;
    private const int FloorplanLayerIndex = 10;
    private const float ExcavationPerSecond = 1f;
    private float ConstructionPerSecond = 1f;

    public Camera AlternativeCamera;
    public Light Headlamp1, Headlamp2;
    /// <summary>
    /// Tube prefab to be created when linking bulkheads
    /// </summary>
    public Transform tubePrefab, gasPipePrefab, powerlinePrefab;
    /// <summary>
    /// the FPS input script (usually on the parent transform)
    /// </summary>
    public CustomFPSController FPSController;
    /// <summary>
    /// The prefab for a construction zone
    /// </summary>
    public Transform ConstructionZonePrefab, PostItNotePrefab;
    /// <summary>
    /// the material to put on module prefabs
    /// when planning where to put them on the ground
    /// </summary>
    public Material translucentPlanningMat;
    
    internal InputMode CurrentMode = InputMode.Normal;
    internal Loadout Loadout = new Loadout();

    internal void SetPressure(bool pressurized)
    {
        FPSController.PlaceBootprints = !pressurized;
    }

    //todo: same kind of cache for floorplan
    private Dictionary<Transform, Transform> FloorplanVisCache = new Dictionary<Transform, Transform>();

    private RoverInput DrivingRoverInput;
    private Collider selectedAirlock1, selectedGasValve, selectedPowerSocket, carriedObject;
    private Matter selectedCompound = Matter.Unspecified;
    private List<Transform> createdTubes = new List<Transform>();
    private List<Transform> createdPipes = new List<Transform>();
    private List<Transform> createdPowerlines = new List<Transform>();
    private bool playerIsOnFoot = true;
    private bool reportMenuOpen = false;

    private bool playerInVehicle
    {
        get
        {
            return !playerIsOnFoot;
        }
    }

    private Planning<Module> ModulePlan = new Planning<Module>();
    private Planning<Stuff> StuffPlan = new Planning<Stuff>();

    private Transform PlannedFloorplanVisualization;
    private Transform lastHobbitHoleTransform;
    private HobbitHole lastHobbitHole;

    private Direction CurrentPlanningDirection;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GuiBridge.Instance.BuildRadialMenu(this.Loadout);
        Equip(Slot.Unequipped);
        PrefabCache<Module>.TranslucentPlanningMat = translucentPlanningMat;
        PrefabCache<Stuff>.TranslucentPlanningMat = translucentPlanningMat;
    }

    // Update is called once per frame
    void Update () {

	    if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (reportMenuOpen)
                ToggleReport(null);
            else if (playerIsOnFoot)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("menu", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else if (playerInVehicle)
            {
                ToggleVehicle(null);
            }
        }


#if UNITY_EDITOR
        if (Input.GetKeyUp(KeyCode.Comma))
        {
            SunOrbit.Instance.SlowDown();
        }
        else if (Input.GetKeyUp(KeyCode.Period))
        {
            SunOrbit.Instance.SpeedUp();
        }
        else if (Input.GetKeyUp(KeyCode.I))
        {
            Loadout.PutEquipmentInSlot(Slot.SecondaryGadget, Equipment.Screwdriver);
        }
        else if (Input.GetKeyUp(KeyCode.B))
        {
            Loadout.PutEquipmentInSlot(Slot.SecondaryGadget, Equipment.Wheelbarrow);
        }
#endif

        if (CurrentMode != InputMode.PostIt)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                GuiBridge.Instance.ToggleRadialMenu(true);
                FPSController.FreezeLook = true;
            }
            else if (Input.GetKeyUp(KeyCode.Tab))
            {
                FPSController.FreezeLook = false;
                Equip(GuiBridge.Instance.ToggleRadialMenu(false));
            }
            else if (GuiBridge.Instance.RadialMenuOpen)
            {
                HandleRadialInput();
            }

            if (Input.GetKeyUp(KeyCode.F))
            {
                Headlamp1.enabled = Headlamp2.enabled = !Headlamp1.enabled;
            }

            if (Input.GetKeyUp(KeyCode.F1))
            {
                GuiBridge.Instance.ToggleHelpMenu();
            }

            if (Input.GetKeyUp(KeyCode.V))
            {
                AlternativeCamera.enabled = !AlternativeCamera.enabled;
            }
        }


        PromptInfo newPrompt = null;
        bool doInteract = Input.GetKeyUp(KeyCode.E);

        switch (CurrentMode)
        {
            case InputMode.Normal:

                HandleDefaultInput(ref newPrompt, doInteract);

                switch (this.Loadout.Equipped)
                {
                    case Equipment.Blueprints:
                        HandleExteriorPlanningInput(ref newPrompt, doInteract);
                        break;
                    case Equipment.Screwdriver:
                        HandleStuffPlanningInput(ref newPrompt, doInteract);
                        break;
                    case Equipment.Wheelbarrow:
                        HandleInteriorPlanningInput(ref newPrompt, doInteract);
                        break;
                }
                break;
            case InputMode.PostIt:
                HandlePostItInput(ref newPrompt, doInteract);
                break;
            case InputMode.Sleep:
                HandleSleepInput(ref newPrompt, doInteract);
                break;
        }

        //if we were hovering or doing something that has a prompt
        //we will have a newPrompt
        //if we don't
        if (newPrompt == null)
        {
            GuiBridge.Instance.HidePrompt();
        }
        else
        {
            GuiBridge.Instance.ShowPrompt(newPrompt);
        }
	}

    private void HandleStuffPlanningInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (StuffPlan.IsActive)
        {
            if (Input.GetMouseButtonUp(0))
            {
                StuffPlan.Rotate(true, false, 45);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                StuffPlan.Rotate(false, false, 45);
            }

            RaycastHit hitInfo;
            if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, "interaction"))
            {
                if (hitInfo.collider != null)
                {
                    if (hitInfo.collider.CompareTag("cavernstuff"))
                    {
                        if (doInteract)
                        {
                            PlaceStuffHere(hitInfo.collider);
                        }
                        else
                        {
                            if (StuffPlan.IsActive && StuffPlan.Visualization.parent != hitInfo.collider.transform)
                            {
                                StuffPlan.Visualization.SetParent(hitInfo.collider.transform);
                                StuffPlan.Visualization.localPosition = Vector3.zero;
                                StuffPlan.Visualization.localRotation = Quaternion.identity;
                            }

                            newPrompt = Prompts.PlaceStuffHint;
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.G))
            {
                FloorplanBridge.Instance.ToggleStuffPanel(true);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                FloorplanBridge.Instance.ToggleStuffPanel(false);
                StuffPlan.Reset();
            }
        }
    }

    private void HandleRadialInput()
    {
        float x = ((Input.mousePosition.x / Screen.width) * 2f) - 1f,
            y = ((Input.mousePosition.y / Screen.height) * 2f) -1f;

        if (x != 0f && y != 0f)
        {
            float theta = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            GuiBridge.Instance.HighlightSector(theta);
        }
    }

    private void HandlePostItInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            this.PostItText = null;
            this.CurrentMode = InputMode.Normal;
            FPSController.SuspendInput = false;

            RefreshEquipmentState();
        }
        else if (this.PostItText != null)
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                //I think text setter appends a newline at the end
                //so you don't delete just len -1, you go back an extra char
                int newLength = PostItText.text.Length - 2;
                //if you're on a newline, go ahead and delete it too
                //or you will be always stuck on this newline
                if (PostItText.text[newLength] == '\n')
                    newLength--;

                this.PostItText.text = WordWrap(this.PostItText, this.PostItText.text.Substring(0, newLength));
            }
            else if (Input.GetKeyUp(KeyCode.Return))
            {
                this.PostItText.text = WordWrap(this.PostItText, this.PostItText.text + '\n');
            }
            else if (Input.anyKeyDown)
            {
                this.PostItText.text = WordWrap(this.PostItText, this.PostItText.text + (Input.inputString ?? "" ).ToLower());
            }

            newPrompt = Prompts.PostItFinishHint;
        }
    }

    //calculated with PermanentMarker font, character size .2, line spacing .6
    private string WordWrap(TextMesh t, string allText)
    {
        string[] lines = allText.Split('\n');
        string lastLine = "";
        bool addLastLine = false;
        for (int i = 0; i < lines.Length; i++)
        {
            print(lines[i].Replace('\n', '*'));
            if (lines[i].Length > 13)
            {
                char orphan = lines[i][lines[i].Length - 1];
                lines[i] = lines[i].Substring(0, lines[i].Length - 1); 
                if (i + 1 < lines.Length)
                {
                    lines[i + 1] = orphan + lines[i + 1];
                }
                else if (lines.Length < 7)
                {
                    lastLine += orphan;
                    addLastLine = true;
                }
            }
        }

        string result = string.Join("\n", lines);

        if (addLastLine)
            result += '\n' + lastLine;

        return result;
    }

    private void HandleInteriorPlanningInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetMouseButtonUp(0))
        {
            CurrentPlanningDirection.Rotate(true);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            CurrentPlanningDirection.Rotate(false);
        }

        if (Input.GetKeyUp(KeyCode.T))
        {
            DisableAndForgetFloorplanVisualization();
            CycleSubGroup();
            Material mat;
            Transform prefab = FloorplanBridge.Instance.GetPrefab(out mat);
            if (prefab != null)
            {
                if (FloorplanVisCache.ContainsKey(prefab))
                {
                    PlannedFloorplanVisualization = FloorplanVisCache[prefab];
                    PlannedFloorplanVisualization.gameObject.SetActive(true);
                }
                else
                {
                    Transform t = GameObject.Instantiate(prefab);
                    FloorplanVisCache[prefab] = t;
                    PlannedFloorplanVisualization = t;
                }
                PlannedFloorplanVisualization.GetChild(0).GetComponent<Renderer>().material = mat;
            }
        }

        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, "interaction"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("cavern"))
                {
                    if (doInteract)
                    {
                        PlaceFloorplanHere(hitInfo.collider);
                    }
                    else
                    {
                        if (PlannedFloorplanVisualization != null)
                        {
                            PlannedFloorplanVisualization.position = hitInfo.collider.transform.parent.position;
                            PlannedFloorplanVisualization.localRotation = CurrentPlanningDirection.ToQuaternion();

                            newPrompt = Prompts.PlaceFloorplanHint;
                        }
                    }
                }
                else if (hitInfo.collider.CompareTag("cavernstuff"))
                {
                    if (doInteract)
                    {
                        PlaceStuffHere(hitInfo.collider);
                    }
                    else
                    {
                        newPrompt = Prompts.PlaceStuffHint;
                    }
                }
            }
        }
    }

    private void CycleSubGroup()
    {
        if (GuiBridge.Instance.selectedFloorplanGroup == FloorplanGroup.Undecided)
        {
            GuiBridge.Instance.selectedFloorplanGroup = FloorplanGroup.Floor;
            GuiBridge.Instance.selectedFloorplanSubgroup = InteriorMap.FloorplanGroupmap[GuiBridge.Instance.selectedFloorplanGroup][0];
        }
        else
        {
            int nextSubGroup = (int)GuiBridge.Instance.selectedFloorplanSubgroup + 1;
            if (nextSubGroup >= InteriorMap.FloorplanGroupmap[GuiBridge.Instance.selectedFloorplanGroup].Length)
            {
                CycleGroup();
            }
            else
            {
                GuiBridge.Instance.selectedFloorplanSubgroup = (FloorplanSubGroup)nextSubGroup;
            }
        }

        GuiBridge.Instance.PlacingPanel.gameObject.SetActive(true);
        GuiBridge.Instance.PlacingText.text = GuiBridge.Instance.selectedFloorplanMaterial + " " + GuiBridge.Instance.selectedFloorplanSubgroup + " " + GuiBridge.Instance.selectedFloorplanGroup;
    }

    private void CycleGroup()
    {
        int nextGroup = (int)GuiBridge.Instance.selectedFloorplanGroup + 1;
        if (nextGroup >= InteriorMap.FloorplanGroupmap.Keys.Count)
        {
            GuiBridge.Instance.selectedFloorplanGroup = FloorplanGroup.Floor;
        }
        else
        {
            GuiBridge.Instance.selectedFloorplanGroup = (FloorplanGroup)nextGroup;
            GuiBridge.Instance.selectedFloorplanSubgroup = InteriorMap.FloorplanGroupmap[GuiBridge.Instance.selectedFloorplanGroup][0];
        }
    }

    private TextMesh PostItText;

    private void PlaceFloorplanHere(Collider place)
    {
        Transform t = GameObject.Instantiate<Transform>(PlannedFloorplanVisualization);
        t.SetParent(place.transform.parent);
        t.localEulerAngles = Round(t.localEulerAngles);
        //DisableAndForgetFloorplanVisualization();
    }

    private void PlaceStuffHere(Collider place)
    {
        Transform t = GameObject.Instantiate<Transform>(PrefabCache<Stuff>.Cache.GetPrefab(StuffPlan.Type));
        t.SetParent(place.transform.parent);
        t.localEulerAngles = Round(t.localEulerAngles);
        t.localPosition = Vector3.down * .5f;
        Equip(Slot.Unequipped);
        StuffPlan.Reset();
    }

    private Vector3 Round(Vector3 localEulerAngles)
    {
        localEulerAngles.x = Mathf.Round(localEulerAngles.x / 90) * 90;
        localEulerAngles.y = Mathf.Round(localEulerAngles.y / 90) * 90;
        localEulerAngles.z = Mathf.Round(localEulerAngles.z / 90) * 90;

        return localEulerAngles;
    }

    private void DisableAndForgetFloorplanVisualization()
    {
        if (PlannedFloorplanVisualization != null)
        {
            PlannedFloorplanVisualization.gameObject.SetActive(false);
            PlannedFloorplanVisualization = null;
        }
    }

    private void HandleDefaultInput(ref PromptInfo newPrompt, bool doInteract)
    {
        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, layerNames: "interaction"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("movable"))
                {
                    if (carriedObject == null)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            PickUpObject(hitInfo);
                        }
                        else
                        {
                            newPrompt = Prompts.PickupHint;
                        }
                    }
                    else
                    {
                        if (Input.GetMouseButtonUp(0))
                        {
                            DropObject();
                        }
                        else
                        {
                            newPrompt = Prompts.DropHint;
                        }
                    }
                }
                else if (hitInfo.collider.gameObject.CompareTag("bulkhead"))
                {
                    newPrompt = OnBulkhead(newPrompt, doInteract, hitInfo);
                }
                else if (hitInfo.collider.gameObject.CompareTag("powerplug"))
                {
                    newPrompt = OnPowerPlug(newPrompt, doInteract, hitInfo);
                }
                else if (IsGasValve(hitInfo.collider))
                {
                    newPrompt = OnGasValve(newPrompt, doInteract, hitInfo);
                }
                else if (hitInfo.collider.gameObject.CompareTag("pipe"))
                {
                    newPrompt = OnExistingPipe(doInteract, hitInfo);
                }
                else if (playerIsOnFoot && hitInfo.collider.gameObject.CompareTag("rover"))
                {
                    if (doInteract)
                    {
                        ToggleVehicle(hitInfo.collider.transform.GetComponent<RoverInput>());
                    }
                    else
                    {
                        newPrompt = Prompts.DriveRoverPrompt;
                    }
                }
                else if (hitInfo.collider.CompareTag("constructionzone"))
                {
                    ConstructionZone zone = hitInfo.collider.GetComponent<ConstructionZone>();

                    if (zone != null && carriedObject == null && zone.CanConstruct)
                    {
                        if (Input.GetKey(KeyCode.E))
                        {
                            zone.WorkOnConstruction(Time.deltaTime * this.ConstructionPerSecond);
                        }
                        Prompts.ConstructHint.Progress = zone.ProgressPercentage;
                        newPrompt = Prompts.ConstructHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("door"))
                {
                    switch (hitInfo.collider.gameObject.name)
                    {
                        case Airlock.LockedDoorName:
                            newPrompt = Prompts.DoorLockedHint;
                            break;
                        case Airlock.OpenDoorName:
                            if (doInteract)
                                Airlock.ToggleDoor(hitInfo.collider.transform);
                            else
                                newPrompt = Prompts.CloseDoorHint;
                            break;
                        case Airlock.ClosedDoorName:
                            if (doInteract)
                                Airlock.ToggleDoor(hitInfo.collider.transform);
                            else
                                newPrompt = Prompts.OpenDoorHint;
                            break;
                    }
                }
                else if (hitInfo.collider.CompareTag("cavernwall"))
                {
                    if (Loadout.Equipped == Equipment.Drill)
                    {
                        if (hitInfo.collider.transform.parent != lastHobbitHoleTransform)
                        {
                            lastHobbitHole = hitInfo.collider.transform.parent.GetComponent<HobbitHole>();
                        }

                        if (lastHobbitHole != null)
                        {
                            if (Input.GetKey(KeyCode.E))
                            {
                                Prompts.ExcavateHint.Progress = lastHobbitHole.Excavate(hitInfo.collider.transform.localPosition, Time.deltaTime * ExcavationPerSecond);
                            }
                            else
                            {
                                Prompts.ExcavateHint.Progress = lastHobbitHole.ExcavationProgress(hitInfo.collider.transform.localPosition);
                            }
                        }
                        else
                        {
                            Prompts.ExcavateHint.Progress = 0f;
                        }

                        newPrompt = Prompts.ExcavateHint;
                    }
                    else
                    {
                        newPrompt = Prompts.DrillHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("button"))
                {
                    if (doInteract)
                    {
                        if (hitInfo.collider.name == "depressurize")
                        {
                            hitInfo.collider.transform.parent.GetComponent<Airlock>().Depressurize();
                        }
                        else if (hitInfo.collider.name == "pressurize")
                        {
                            hitInfo.collider.transform.parent.GetComponent<Airlock>().Pressurize();
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.GenericButtonHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("water"))
                {
                    if (doInteract)
                    {
                        SurvivalTimer.Instance.FillWater();
                    }
                    else
                    {
                        newPrompt = Prompts.DrinkWaterHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("ladder"))
                {
                    if (doInteract && !FPSController.m_IsTransitioningLadder)
                    {
                        if (FPSController.m_IsOnLadder)
                            FPSController.GetOffLadder();
                        else
                        {
                            FPSController.GetOnLadder(hitInfo.collider.transform.GetChild(0).position, hitInfo.collider.transform.GetChild(1).position.y);
                        }
                    }
                    else
                    {
                        if (FPSController.m_IsOnLadder)
                            newPrompt = Prompts.LadderOffHint;
                        else
                            newPrompt = Prompts.LadderOnHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("foodprep"))
                {
                    //todo: refactor - weak link on name
                    bool isPowder = hitInfo.collider.name == "powder";
                    bool isBiomass = hitInfo.collider.name == "biomass";
                    //todo: bug - shows prompts even when actions not available due to availability/storage limits
                    if (doInteract)
                    {
                        if (isPowder)
                            hitInfo.collider.transform.root.GetComponent<Habitat>().PreparePowderToShake();
                        else if (isBiomass)
                            hitInfo.collider.transform.root.GetComponent<Habitat>().PrepareBiomassToPreparedMeal();
                    }
                    else
                    {
                        if (isPowder)
                            newPrompt = Prompts.FoodPrepPowderHint;
                        else if (isBiomass)
                            newPrompt = Prompts.FoodPrepBiomassHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("mealorganic"))
                {
                    newPrompt = OnFoodHover(doInteract, Prompts.MealOrganicEatHint, MealType.Organic);
                }
                else if (hitInfo.collider.CompareTag("mealprepared"))
                {
                    newPrompt = OnFoodHover(doInteract, Prompts.MealPreparedEatHint, MealType.Prepared);
                }
                else if (hitInfo.collider.CompareTag("mealshake"))
                {
                    newPrompt = OnFoodHover(doInteract, Prompts.MealShakeEatHint, MealType.Shake);
                }
                else if (hitInfo.collider.CompareTag("bed"))
                {
                    if (doInteract)
                    {
                        BeginSleep(hitInfo.collider.transform.GetChild(0), hitInfo.collider.transform.GetChild(1));
                    }
                    else
                    {
                        newPrompt = Prompts.BedEnterHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("postit"))
                {
                    if (doInteract)
                    {
                        GameObject.Destroy(hitInfo.collider.gameObject);
                    }
                    else
                    {
                        newPrompt = Prompts.PostItDeleteHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("evacharger"))
                {
                    if (Input.GetKey(KeyCode.E))
                    {
                        SurvivalTimer.Instance.Power.Resupply(EVAChargerPerSecond * Time.deltaTime);
                    }
                    else
                    {
                        newPrompt = Prompts.EVAChargeHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("network"))
                {
                    if (doInteract)
                    {
                        if (reportMenuOpen)
                        {
                            ToggleReport(null);
                        }
                        else
                        {
                            ToggleReport(hitInfo.collider.transform.root.GetComponent<ModuleGameplay>());
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.ReportHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("powerSwitch"))
                {
                    if (doInteract)
                    {
                        hitInfo.collider.transform.root.GetComponent<IPowerToggleable>().TogglePower();
                    }
                    else
                    {
                        if (hitInfo.collider.name == "on")
                        {
                            newPrompt = Prompts.PowerSwitchOffHint;
                        }
                        else
                        {
                            newPrompt = Prompts.PowerSwitchOnHint;
                        }
                    }
                }
                else if (hitInfo.collider.CompareTag("airbagpayload"))
                {
                    if (doInteract)
                    {
                        //bouncelander is on parent of interaction cube
                        BounceLander landerScript = hitInfo.collider.transform.parent.GetComponent<BounceLander>();

                        if (landerScript != null)
                        {
                            landerScript.Disassemble();
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.PayloadDisassembleHint;
                    }
                }
            }
            else if (doInteract)
            {
                if (selectedAirlock1 != null)
                {
                    selectedAirlock1 = null;
                }
                if (selectedGasValve != null)
                {
                    selectedGasValve = null;
                }
            }
        }
        //if we raycast, and DO NOT hit our carried object, it has gotten moved because of physics
        //so drop it!
        else if (carriedObject != null)
        {
            DropObject();
        }
        else if (FPSController.m_IsOnLadder)
        {
            if (doInteract)
            {
                FPSController.GetOffLadder();
            }
            else
            {
                newPrompt = Prompts.LadderOffHint;
            }
        }

        if (!doInteract && Input.GetKeyUp(KeyCode.P))
        {
            PlacePostIt();
        }
    }

    private void ToggleReport(ModuleGameplay moduleGameplay)
    {
        if (moduleGameplay == null)
        {
            GuiBridge.Instance.ToggleReportMenu(false);
            reportMenuOpen = false;
        }
        else
        {
            moduleGameplay.Report();
            reportMenuOpen = true;
        }
    }

    private void PlacePostIt()
    {
        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, "Default", "interaction"))
        {
            Transform t = GameObject.Instantiate(PostItNotePrefab, hitInfo.point, Quaternion.LookRotation(hitInfo.normal)) as Transform;
            t.SetParent(hitInfo.collider.transform);
            this.PostItText = t.GetChild(0).GetChild(0).GetComponent<TextMesh>();
            this.CurrentMode = InputMode.PostIt;
            FPSController.SuspendInput = true;
            this.RefreshEquipmentState();
        }
    }

    private PromptInfo OnExistingPipe(bool doInteract, RaycastHit hitInfo)
    {
        if (doInteract)
        {
            //pipe script is on parent object
            Pipe pipeScript = hitInfo.collider.transform.parent.GetComponent<Pipe>();
            ModuleGameplay from = pipeScript.from.root.GetComponent<ModuleGameplay>();
            ModuleGameplay to = pipeScript.to.root.GetComponent<ModuleGameplay>();

            if (from == null || to == null)
            {
                UnityEngine.Debug.LogWarning("Pipe not connected to two modules!");
            }
            else
            {
                from.UnlinkFromModule(to);
                to.UnlinkFromModule(from);
            }
            //pipe root is on parent object
            GameObject.Destroy(hitInfo.collider.transform.parent.gameObject);
            return null;
        }
        else
        {
            return Prompts.ExistingPipeRemovalHint;
        }
    }

    private void HandleExteriorPlanningInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (ModulePlan.IsActive)
        {
            if (Input.GetMouseButton(0))
            {
                ModulePlan.Rotate(true);
            }
            else if (Input.GetMouseButton(1))
            {
                ModulePlan.Rotate(false);
            }
        } else {
            if (Input.GetKeyUp(KeyCode.Q))
            {
                GuiBridge.Instance.CycleConstruction(-1);
            }
            else if (Input.GetKeyUp(KeyCode.Z))
            {
                GuiBridge.Instance.CycleConstruction(1);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                GuiBridge.Instance.SelectConstructionPlan(0);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                GuiBridge.Instance.SelectConstructionPlan(1);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha3))
            {
                GuiBridge.Instance.SelectConstructionPlan(2);
            }
            else if (Input.GetKeyUp(KeyCode.Alpha4))
            {
                GuiBridge.Instance.SelectConstructionPlan(3);
            }
        }

        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Ignore, "Default"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.CompareTag("terrain"))
                {
                    if (Loadout.Equipped == Equipment.Blueprints && ModulePlan.IsActive)
                    {
                        //TODO: raycast 3 more times (other 3 corners)
                        //then take the average height between them
                        //and invalidate the placement if it passes some threshold
                        ModulePlan.Visualization.position = hitInfo.point;

                        if (doInteract)
                        {
                            PlaceConstructionHere(hitInfo.point);
                        }
                        else
                        {
                            newPrompt = Prompts.PlanConstructionZoneHint;
                        }
                    }
                }
            }
        }
    }

    private bool CastRay(out RaycastHit hitInfo, QueryTriggerInteraction triggerInteraction, params string[] layerNames)
    {
        return Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, InteractionRaycastDistance, LayerMask.GetMask(layerNames), triggerInteraction);
    }

    private PromptInfo OnFoodHover(bool doInteract, PromptInfo eatHint, MealType mealType)
    {
        if (doInteract)
        {
            SurvivalTimer.Instance.EatFood(mealType);
            return null;
        }
        else
        {
            return eatHint;
        }
    }

    private bool IsGasValve(Collider collider)
    {
        return collider.CompareTag("valve") ||
            collider.CompareTag("hydrogenvalve") ||
            collider.CompareTag("oxygenvalve") ||
            collider.CompareTag("methanevalve") ||
            collider.CompareTag("watervalve") ||
            collider.CompareTag("carbondioxidevalve");
    }

    private PromptInfo OnPowerPlug(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedPowerSocket, value => selectedPowerSocket = value, PlacePowerPlug, Prompts.PowerPlugPrompts);
    }

    private PromptInfo OnBulkhead(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedAirlock1, value => selectedAirlock1 = value, PlaceTube, Prompts.BulkheadBridgePrompts);
    }

    private PromptInfo OnGasValve(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        Matter other = GetCompoundFromValve(hitInfo.collider);

        if (selectedCompound != Matter.Unspecified)
        {
            if (!CompoundsMatch(selectedCompound, other))
            {
                return Prompts.InvalidPipeHint;
            }
        }

        return OnLinkable(doInteract, hitInfo, selectedGasValve, value => 
        {
            selectedGasValve = value;

            if (value == null)
                //todo: bug this actually should be nullable
                selectedCompound = Matter.Unspecified;
            else 
                selectedCompound = other;

        }, PlaceGasPipe, Prompts.GasPipePrompts);
    }

    private static bool CompoundsMatch(Matter selectedCompound, Matter other)
    {
        if (selectedCompound == Matter.Unspecified &&
            other == Matter.Unspecified)
        {
            return true;
        }
        else if (selectedCompound != Matter.Unspecified &&
            other == Matter.Unspecified)
        {
            return true;
        }
        else
        {
            return selectedCompound == other;
        }
    }

    private static Matter GetCompoundFromValve(Collider collider)
    {
        switch (collider.tag)
        {
            case "oxygenvalve":
                return Matter.Oxygen;
            case "hydrogenvalve":
                return Matter.Hydrogen;
            case "methanevalve":
                return Matter.Methane;
            case "carbondioxidevalve":
                return Matter.CarbonDioxide;
            case "watervalve":
                return Matter.Water;
            default:
                return Matter.Unspecified;
        }
    }

    //todo: move out of this class
    public static string GetValveFromCompound(Matter c)
    {
        switch (c)
        {
            case Matter.Oxygen:
                return "oxygenvalve";
            case Matter.Hydrogen:
                return "hydrogenvalve";
            case Matter.Methane:
                return "methanevalve";
            case Matter.CarbonDioxide:
                return "carbondioxidevalve";
            case Matter.Water:
                return "watervalve";
            default:
                return "valve";
        }
    }

    private static PromptInfo OnLinkable(bool doInteract, RaycastHit hitInfo, Collider savedLinkEnd, Action<Collider> SetSaved, Action<Collider> OnLinkPlaced, LinkablePrompts promptGroup )
    {
        PromptInfo newPrompt = null;

        if (doInteract)
        {
            if (savedLinkEnd == null)
            {
                SetSaved(hitInfo.collider);
                //maybe not this? maybe null?
                //maybe a prompt instead of a hint
                newPrompt = promptGroup.HoverWhenOneSelected;
            }
            else if (savedLinkEnd != hitInfo.collider)
            {
                OnLinkPlaced(hitInfo.collider);
                SetSaved(null);
                GuiBridge.Instance.ShowNews(promptGroup.WhenCompleted);
            }
        }
        else
        {
            if (savedLinkEnd == null)
            {
                newPrompt = promptGroup.HoverWhenNoneSelected;
            }
            else if (savedLinkEnd != hitInfo.collider)
            {
                newPrompt = promptGroup.HoverWhenOneSelected;
            }
        }

        return newPrompt;
    }

    private void PlaceConstructionHere(Vector3 point)
    {
        Transform zoneT = (Transform)GameObject.Instantiate(ConstructionZonePrefab, ModulePlan.Visualization.position, ModulePlan.Visualization.rotation);

        ConstructionZone zone = zoneT.GetComponent<ConstructionZone>();

        zone.Initialize(ModulePlan.Type);

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt))
            zone.Complete();

        ModulePlan.Reset();
        Equip(Slot.Unequipped);
    }

    private void Equip(Slot s)
    {
        if (Loadout[s] == Equipment.Locked)
            s = Slot.Unequipped;

        Loadout.ActiveSlot = s;

        RefreshEquipmentState();
    }

    private void RefreshEquipmentState()
    {
        switch (Loadout.Equipped)
        {
            case Equipment.ChemicalSniffer:
                AlternativeCamera.cullingMask = 1 << ChemicalFlowLayerIndex;
                AlternativeCamera.enabled = true;
                break;
            case Equipment.Blueprints:
                AlternativeCamera.cullingMask = 1 << ChemicalFlowLayerIndex;
                AlternativeCamera.enabled = true;
                break;
            case Equipment.Screwdriver:
            case Equipment.Wheelbarrow:
                this.FPSController.FreezeLook = true;
                AlternativeCamera.cullingMask = 1 << FloorplanLayerIndex;
                AlternativeCamera.enabled = true;
                FloorplanBridge.Instance.ToggleStuffPanel(Loadout.Equipped == Equipment.Screwdriver);
                break;
            default:
                DisableAndForgetFloorplanVisualization();
                AlternativeCamera.enabled = false;
                break;
        }
        GuiBridge.Instance.RefreshMode();
    }

    private void PickUpObject(RaycastHit hitInfo)
    {
        carriedObject = hitInfo.collider;
        carriedObject.GetComponent<Rigidbody>().useGravity = false;
        carriedObject.transform.SetParent(this.transform);
    }

    private void DropObject()
    {
        carriedObject.GetComponent<Rigidbody>().useGravity = true;
        carriedObject.transform.SetParent(null);
        carriedObject = null;
    }

    private void ToggleVehicle(RoverInput roverInput)
    {
        //exiting vehicle
        if (roverInput == null && DrivingRoverInput != null)
        {
            playerIsOnFoot = true;
            DrivingRoverInput.enabled = false;
            FPSController.transform.position = DrivingRoverInput.transform.Find("Exit").transform.position;
            FPSController.transform.SetParent(null);
            FPSController.SuspendInput = false;
        }
        else //entering vehicle
        {
            playerIsOnFoot = false;
            //FPSController.enabled = false;
            DrivingRoverInput = roverInput;
            DrivingRoverInput.enabled = true;
            FPSController.transform.SetParent(DrivingRoverInput.transform.Find("Enter").transform);
            FPSController.transform.localPosition = Vector3.zero;
            FPSController.transform.localRotation = Quaternion.identity;
            FPSController.InitializeMouseLook();
            FPSController.SuspendInput = true;
        }
    }

    private void PlaceTube(Collider collider)
    {
        PlaceRuntimeLinkingObject(selectedAirlock1, collider, tubePrefab, createdTubes, true, .2f);
    }

    private void PlaceGasPipe(Collider collider)
    {
        Transform newPipe = PlaceRuntimeLinkingObject(selectedGasValve, collider, gasPipePrefab, createdPipes);

        if (selectedCompound == Matter.Unspecified)
            selectedCompound = GetCompoundFromValve(collider);

        ModuleGameplay g1 = selectedGasValve.transform.root.GetComponent<ModuleGameplay>(), g2 = collider.transform.root.GetComponent<ModuleGameplay>();
        if (g1 != null && g2 != null)
        {
            if (g2 is GasStorage)
            {
                (g2 as GasStorage).SpecifyCompound(selectedCompound);
            }
            else if (g1 is GasStorage)
            {
                (g1 as GasStorage).SpecifyCompound(selectedCompound);
            }
            g1.LinkToModule(g2);
            g2.LinkToModule(g1);
        }

        Pipe pipeScript = newPipe.GetComponent<Pipe>();
        pipeScript.PipeType = selectedCompound;
        pipeScript.from = selectedGasValve.transform;
        pipeScript.to = collider.transform;

        selectedCompound = Matter.Unspecified;
    }

    private void PlacePowerPlug(Collider collider)
    {
        PlaceRuntimeLinkingObject(selectedPowerSocket, collider, powerlinePrefab, createdPowerlines);

        //turn on "plug" cylinders
        collider.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
        selectedPowerSocket.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;

        ModuleGameplay g1 = selectedPowerSocket.transform.root.GetComponent<ModuleGameplay>(), g2 = collider.transform.root.GetComponent<ModuleGameplay>();
        if (g1 != null && g2 != null)
        {
            if (g1.HasPower || g2.HasPower)
            {
                g1.HasPower = g2.HasPower = true;
            }
        }
    }

    private static Transform PlaceRuntimeLinkingObject(Collider firstObject, Collider otherObject, Transform linkingObjectPrefab, List<Transform> addToList, bool hideObjectEnds = false, float extraScale = 0f)
    {
        float distanceBetween = Vector3.Distance(firstObject.transform.position, otherObject.transform.position);

        Vector3 midpoint = Vector3.Lerp(firstObject.transform.position, otherObject.transform.position, 0.5f);
        Transform newTube = GameObject.Instantiate<Transform>(linkingObjectPrefab);

        newTube.position = midpoint;
        newTube.LookAt(otherObject.transform);
        newTube.localScale = new Vector3(newTube.localScale.x, newTube.localScale.y, (distanceBetween / 2f) + extraScale);
        addToList.Add(newTube);

        if (hideObjectEnds)
        {
            firstObject.gameObject.SetActive(false);
            otherObject.gameObject.SetActive(false);
        }

        return newTube;
    }

    internal void PlanModule(Module planModule)
    {
        this.ModulePlan.SetVisualization(planModule);
    }

    internal void PlanStuff(Stuff s)
    {
        this.StuffPlan.SetVisualization(s);
    }

    public void KillPlayer()
    {
        GuiBridge.Instance.ShowKillMenu();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        FPSController.enabled = false;
        this.enabled = false;
    }

    #region sleep mechanic
    private SleepLerpContext sleerpCtx;
    private Vector2 lastRadialInputDirection;

    private struct SleepLerpContext
    {
        public Vector3 FromPosition, ToPosition;
        public Quaternion FromRotation, ToRotation;
        public float Duration;
        private float Time;
        public Transform SleepExit;
        public bool Done;

        public void StandUp()
        {
            FromPosition = ToPosition;
            FromRotation = ToRotation;
            ToPosition = SleepExit.position;
            ToRotation = SleepExit.rotation;
            Done = false;
            Time = 0f;
        }

        public void Tick(Transform body, Transform camera)
        {
            this.Time += UnityEngine.Time.deltaTime;
            if (this.Time > this.Duration)
            {
                body.position = ToPosition;
                //camera.rotation = ToRotation;
                Done = true;
            }
            else
            {
                body.position = Vector3.Lerp(FromPosition, ToPosition, Time / Duration);
                //camera.rotation = Quaternion.Lerp(FromRotation, ToRotation, Time / Duration);
            }
        }
    }

    private void BeginSleep(Transform enterTranform, Transform exitTransform)
    {
        sleerpCtx = new SleepLerpContext()
        {
            FromPosition = FPSController.transform.position,
            ToPosition = enterTranform.position,
            FromRotation = Camera.main.transform.rotation,
            ToRotation = enterTranform.rotation,
            Duration = .5f,
            SleepExit = exitTransform
        };

        ToggleSleep(true);

        StartCoroutine(BedLerp());
    }

    private void ExitSleep()
    {
        ToggleSleep(false);
    }

    private void ToggleSleep(bool isAsleep)
    {
        this.CurrentMode = isAsleep ? InputMode.Sleep : InputMode.Normal;
        FPSController.SuspendInput = isAsleep;
        this.RefreshEquipmentState();
    }

    private void HandleSleepInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Comma))
        {
            SunOrbit.Instance.SlowDown();
        }
        else if (Input.GetKeyUp(KeyCode.Period))
        {
            SunOrbit.Instance.SpeedUp();
        }

        if (doInteract)
        {
            sleerpCtx.StandUp(); //reset ctx
            StartCoroutine(BedLerp(true));
        }
        else
        {
            newPrompt = Prompts.BedExitHint;
        }
    }

    private IEnumerator BedLerp(bool exitStateOnDone = false)
    {
        while (!sleerpCtx.Done)
        {
            sleerpCtx.Tick(FPSController.transform, Camera.main.transform);

            //every time we modify the camera via script we need to do this call
            FPSController.InitializeMouseLook();

            yield return null;
        }

        if (exitStateOnDone)
        {
            ExitSleep();
        }
    }
    #endregion
}
