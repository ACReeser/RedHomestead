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
using RedHomestead.Persistence;
using RedHomestead.Electricity;
using RedHomestead.Perks;
using RedHomestead.Industry;
using RedHomestead.Crafting;
using RedHomestead.Agriculture;

[Serializable]
public struct InteractionClips
{
    public AudioClip Drill, Construction;
}

/// <summary>
/// Responsible for raycasting, modes, and gameplay input
/// </summary>
public class PlayerInput : MonoBehaviour {
    public static PlayerInput Instance;

    public enum InputMode { Menu = -1, Normal = 0, PostIt, Sleep, Terminal, Pipeline, Powerline, Crafting }

    private const float InteractionRaycastDistance = 10f;
    private const int ChemicalFlowLayerIndex = 9;
    private const int FloorplanLayerIndex = 10;
    
#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
    public static bool DoNotDisturb;
#endif

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
    public Transform PostItNotePrefab;
    /// <summary>
    /// the material to put on module prefabs
    /// when planning where to put them on the ground
    /// </summary>
    public Material translucentPlanningMat;
    public ParticleSystem DrillSparks;

    public AudioSource InteractionSource;
    public InteractionClips Sfx;
    public AudioClip GoodMorningHomesteader;

    internal InputMode CurrentMode = InputMode.Normal;
    internal Loadout Loadout = new Loadout();
    internal bool LookForRepairs = false;

    internal void SetPressure(bool pressurized)
    {
        FPSController.PlaceBootprints = !pressurized;
    }

    private RoverInput DrivingRoverInput;
    private Collider selectedBulkhead, selectedGasValve, selectedPowerSocket;
    private Rigidbody carriedObject;
    private Matter selectedCompound = Matter.Unspecified;
    private List<Transform> createdTubes = new List<Transform>();
    private List<Transform> createdPipes = new List<Transform>();
    private List<Transform> createdPowerlines = new List<Transform>();
    internal bool IsOnFoot { get; private set; }
    internal bool IsInSuit { get; private set; }
    private bool reportMenuOpen = false;

    internal bool IsInVehicle
    {
        get
        {
            return !IsOnFoot;
        }
    }
    internal bool IsInShirtsleeves
    {
        get
        {
            return !IsInSuit;
        }
    }

    private Workshop CurrentCraftablePlanner;
    private Planning<Module> ModulePlan = new Planning<Module>();
    private Planning<Stuff> StuffPlan = new Planning<Stuff>();
    private Planning<Floorplan> FloorPlan = new Planning<Floorplan>();

    private Transform lastHobbitHoleTransform;
    private HobbitHole lastHobbitHole;

    void Awake()
    {
        Instance = this;
        InteractionSource.transform.SetParent(null);
        IsOnFoot = true;
    }

    void Start()
    {
        GuiBridge.Instance.BuildRadialMenu(this.Loadout);
        Equip(Slot.Unequipped);
        PrefabCache<Module>.TranslucentPlanningMat = translucentPlanningMat;
        PrefabCache<Stuff>.TranslucentPlanningMat = translucentPlanningMat;
        PrefabCache<Floorplan>.TranslucentPlanningMat = translucentPlanningMat;
        Autosave.Instance.AutosaveEnabled = true;
        DrillSparks.transform.SetParent(null);
        GuiBridge.Instance.RefreshSurvivalPanel(false);
    }

    internal void PlanCraftable(Craftable whatToBuild)
    {
        CurrentCraftablePlanner.SetCurrentCraftable(whatToBuild);
        CurrentCraftablePlanner.ToggleCraftableView(true);
        this.CurrentMode = InputMode.Crafting;
    }

    // Update is called once per frame
    void Update () {
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
        else if (Input.GetKeyUp(KeyCode.S) && Input.GetKey(KeyCode.RightShift))
        {
            Autosave.Instance.Save();
        }
#endif

#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
        if (Input.GetKeyUp(KeyCode.End) && Input.GetKey(KeyCode.LeftShift)) DoNotDisturb = !DoNotDisturb;
#endif

        if (Input.GetKeyUp(KeyCode.F1))
        {
            GuiBridge.Instance.ToggleHelpMenu();
        } else if (Input.GetKeyUp(KeyCode.F2))
        {
            GuiBridge.Instance.ToggleCinematicMode();
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
                    case Equipment.Wrench:
                        HandleWrenchInput(ref newPrompt);
                        break;
                }

                if (CurrentCraftablePlanner != null)
                {
                    if (Input.GetKeyUp(KeyCode.G))
                    {
                        ToggleCraftableBlueprintMode(true);
                    }
                    else if (Input.GetKeyDown(KeyCode.Tab))
                    {
                        ToggleCraftableBlueprintMode(false);
                    }
                }
                break;
            case InputMode.PostIt:
                HandlePostItInput(ref newPrompt, doInteract);
                break;
            case InputMode.Sleep:
                HandleSleepInput(ref newPrompt, doInteract);
                break;
            case InputMode.Crafting:
                HandleCraftingInput(ref newPrompt, doInteract);
                break;
            case InputMode.Terminal:
                HandleTerminalInput(ref newPrompt, doInteract);
                break;
            case InputMode.Pipeline:
                HandlePipelineInput(ref newPrompt, doInteract);
                break;
            case InputMode.Powerline:
                HandlePowerlineInput(ref newPrompt, doInteract);
                break;
            case InputMode.Menu:
                if (Input.GetKeyUp(KeyCode.Escape))
                    ToggleMenu();
                break;
        }


        if (CurrentEVAStation != null && doInteract)
        {
            CurrentEVAStation.ToggleUse(false);
            CurrentEVAStation = null;
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

    private void HandleCraftingInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Comma))
        {
            SunOrbit.Instance.SlowDown();
        }
        else if (Input.GetKeyUp(KeyCode.Period))
        {
            SunOrbit.Instance.SpeedUp();
        }
        else if (Input.GetKeyUp(KeyCode.Escape))
        {
            wakeyWakeySignal = WakeSignal.PlayerCancel;
        }

        if (CurrentCraftablePlanner != null)
        {
            CurrentCraftablePlanner.MakeProgress(Time.deltaTime);
        }

        if (wakeyWakeySignal.HasValue && wakeyWakeySignal.Value != WakeSignal.DayStart)
        {
            CurrentCraftablePlanner.ToggleCraftableView(false);
            
            ToggleCraftableBlueprintMode(false);

            wakeyWakeySignal = null;

            CurrentMode = InputMode.Normal;
        }
    }

    private void HandleWrenchInput(ref PromptInfo newPrompt)
    {
        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Ignore, layerNames: "Default"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.transform.root.CompareTag(Gremlin.GremlindTag))
                {
                    IRepairable repairable = hitInfo.collider.transform.root.GetComponent<IRepairable>();

                    if (repairable != null)
                    {
                        if (Input.GetKey(KeyCode.E))
                        {
                            Gremlin.Instance.EffectRepair(repairable);
                        }

                        newPrompt = Prompts.RepairHint;
                        newPrompt.Progress = 1f - repairable.FaultedPercentage;
                    }
                }
            }
        }
    }

    private void HandlePowerlineInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            selectedPowerSocket = null;
            CurrentMode = InputMode.Normal;
            GuiBridge.Instance.RefreshMode();
        }

        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, layerNames: "interaction"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("powerplug"))
                {
                    newPrompt = OnPowerPlug(newPrompt, doInteract, hitInfo);
                }
                else
                {
                    newPrompt = Prompts.StopPowerPlugHint;
                }
            }
            else
            {
                newPrompt = Prompts.StopPowerPlugHint;
            }
        }
        else
        {
            newPrompt = Prompts.StopPowerPlugHint;
        }
    }

    private void HandlePipelineInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            selectedGasValve = null;
            CurrentMode = InputMode.Normal;
            GuiBridge.Instance.RefreshMode();
        }

        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, layerNames: "interaction"))
        {
            if (hitInfo.collider != null)
            {
                if (IsGasValve(hitInfo.collider))
                {
                    newPrompt = OnGasValve(newPrompt, doInteract, hitInfo);
                }
                else
                {
                    newPrompt = Prompts.StopGasPipeHint;
                }
            }
            else
            {
                newPrompt = Prompts.StopGasPipeHint;
            }
        }
        else
        {
            newPrompt = Prompts.StopGasPipeHint;
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
            FPSController.FreezeMovement = false;

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
        if (FloorPlan.IsActive)
        {
            if (Input.GetMouseButtonUp(0))
            {
                FloorPlan.Rotate(true, false);
            }
            else if (Input.GetMouseButtonUp(1))
            {
                FloorPlan.Rotate(false, false);
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
                            if (FloorPlan.IsActive && FloorPlan.Visualization.parent != hitInfo.collider.transform)
                            {
                                FloorPlan.Visualization.SetParent(hitInfo.collider.transform);
                                FloorPlan.Visualization.localPosition = Vector3.zero;
                                FloorPlan.Visualization.localRotation = Quaternion.identity;

                                newPrompt = Prompts.PlaceFloorplanHint;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.G))
            {
                FloorplanBridge.Instance.ToggleFloorplanPanel(true);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                FloorplanBridge.Instance.ToggleFloorplanPanel(false);
                FloorPlan.Reset();
            }
        }
    }
    
    private TextMesh PostItText;

    private void PlaceFloorplanHere(Collider place)
    {
        Transform t = GameObject.Instantiate<Transform>(PrefabCache<Floorplan>.Cache.GetPrefab(FloorPlan.Type), FloorPlan.Visualization.position, FloorPlan.Visualization.rotation);
        t.GetChild(0).GetComponent<MeshRenderer>().material = FloorplanBridge.Instance.ConcreteMaterial;
        t.SetParent(place.transform.parent);
        t.localEulerAngles = Round(t.localEulerAngles);
        t.localPosition = Vector3.zero;
        Equip(Slot.Unequipped);
        FloorPlan.Reset();
    }

    private void PlaceStuffHere(Collider place)
    {
        Transform t = GameObject.Instantiate<Transform>(PrefabCache<Stuff>.Cache.GetPrefab(StuffPlan.Type), StuffPlan.Visualization.position, StuffPlan.Visualization.rotation);
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
    
    private void HandleDefaultInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (reportMenuOpen)
            {
                ToggleReport(null);
            }
            else if (IsInVehicle)
            {
                ToggleVehicle(null);
            }
            else
            {
                ToggleMenu();
                return;
            }
        }

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
        if (Input.GetKeyUp(KeyCode.V))
        {
            AlternativeCamera.enabled = !AlternativeCamera.enabled;
        }
        if (IsInVehicle && Input.GetKeyUp(KeyCode.C))
        {
            DrivingRoverInput.ChangeCameraMount();
        }

        RaycastHit hitInfo;
        if (CastRay(out hitInfo, carriedObject == null ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore, layerNames: "interaction"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("movable"))
                {
                    IMovableSnappable res = hitInfo.collider.GetComponent<IMovableSnappable>();

                    if (res == null)
                        res = hitInfo.collider.transform.root.GetComponent<IMovableSnappable>();

                    bool isDeployable = res is IDeployable;

                    if (carriedObject == null)
                    {
                        if (Input.GetMouseButtonDown(0) && (!isDeployable || !(res as IDeployable).Deployed))
                        {
                            PickUpObject(hitInfo.rigidbody, res);
                        }
                        else if (isDeployable)
                        {
                            if (doInteract)
                                (res as IDeployable).ToggleDeploy();
                            else
                            {
                                newPrompt = (res as IDeployable).Deployed ? Prompts.DeployableRetractHint : Prompts.DeployableDeployHint;
                            }
                        }
                        else
                        {
                            newPrompt = Prompts.PickupHint;
                            newPrompt.ItalicizedText = res.GetText();
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
                            if (res != null)
                                newPrompt.ItalicizedText = res.GetText();
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
                else if (hitInfo.collider.gameObject.CompareTag("powerline"))
                {
                    newPrompt = OnExistingPowerline(doInteract, hitInfo);
                }
                else if (IsOnFoot && hitInfo.collider.gameObject.CompareTag("rover"))
                {
                    RoverInput ri = hitInfo.collider.transform.GetComponent<RoverInput>();

                    if (ri.CanDrive)
                    {
                        if (doInteract)
                            ToggleVehicle(ri);
                        else
                            newPrompt = Prompts.DriveRoverPrompt;
                    }
                    else
                    {
                        newPrompt = Prompts.UnhookRoverPrompt;
                    }
                }
                else if (hitInfo.collider.CompareTag("constructionzone"))
                {
                    ConstructionZone zone = hitInfo.collider.GetComponent<ConstructionZone>();

                    if (zone != null && carriedObject == null)
                    {
                        if (Input.GetKeyUp(KeyCode.X))
                        {
                            zone.Deconstruct();
                        }
                        else if (zone.CanConstruct)
                        {
                            if (Loadout.Equipped == Equipment.PowerDrill)
                            {
                                if (Input.GetMouseButtonDown(0))
                                    PlayInteractionClip(zone.transform.position, Sfx.Construction);

                                if (Input.GetMouseButton(0))
                                {
                                    zone.WorkOnConstruction(Time.deltaTime * PerkMultipliers.ConstructSpeed);
                                }

                                Prompts.ConstructHint.Progress = zone.ProgressPercentage;
                                newPrompt = Prompts.ConstructHint;
                            }
                            else
                            {
                                newPrompt = Prompts.PowerDrillHint;
                            }
                        }
                        else
                        {
                            newPrompt = Prompts.DeconstructHint;
                        }
                    }
                }
                else if (hitInfo.collider.CompareTag("door"))
                {
                    IDoorManager doorM = hitInfo.collider.transform.root.GetComponent<IDoorManager>();

                    switch (hitInfo.collider.gameObject.name)
                    {
                        case Airlock.LockedDoorName:
                            newPrompt = Prompts.DoorLockedHint;
                            break;
                        case Airlock.OpenDoorName:
                            if (doInteract)
                                doorM.ToggleDoor(hitInfo.collider.transform);
                            else
                                newPrompt = Prompts.CloseDoorHint;
                            break;
                        case Airlock.ClosedDoorName:
                        default:
                            if (doInteract)
                                doorM.ToggleDoor(hitInfo.collider.transform);
                            else
                                newPrompt = Prompts.OpenDoorHint;
                            break;
                    }
                }
                else if (IsOnFoot && hitInfo.collider.gameObject.CompareTag("hatchback"))
                {
                    if (doInteract)
                    {
                        hitInfo.collider.transform.root.GetComponent<RoverInput>().ToggleHatchback();
                    }
                    else
                    {
                        newPrompt = Prompts.RoverDoorPrompt;
                    }
                }
                else if (hitInfo.collider.CompareTag("cavernwall"))
                {
                    if (Loadout.Equipped == Equipment.PowerDrill)
                    {
                        if (hitInfo.collider.transform.parent != lastHobbitHoleTransform)
                        {
                            lastHobbitHole = hitInfo.collider.transform.parent.GetComponent<HobbitHole>();
                        }

                        if (lastHobbitHole != null)
                        {
                            if (Input.GetKeyDown(KeyCode.E))
                            {
                                PlayInteractionClip(hitInfo.collider.transform.position, Sfx.Drill, false);
                                DrillSparks.Play();
                            }

                            if (Input.GetKey(KeyCode.E))
                            {
                                Prompts.ExcavateHint.Progress = lastHobbitHole.Excavate(hitInfo.collider.transform.localPosition, Time.deltaTime * PerkMultipliers.ExcavationSpeed);
                                if (Prompts.ExcavateHint.Progress >= 1f)
                                {
                                    DrillSparks.Stop();
                                    InteractionSource.Stop();
                                }
                                else
                                {
                                    DrillSparks.transform.position = hitInfo.point + hitInfo.normal.normalized * .02f;
                                }
                            }
                            else
                            {
                                DrillSparks.Stop();
                                InteractionSource.Stop();
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
                            hitInfo.collider.transform.parent.parent.GetComponent<Airlock>().Depressurize();
                        }
                        else if (hitInfo.collider.name == "pressurize")
                        {
                            hitInfo.collider.transform.parent.parent.GetComponent<Airlock>().Pressurize();
                        }
                        else if (hitInfo.collider.name == "RoverLightsButton")
                        {
                            DrivingRoverInput.ToggleLights();
                        }
                        else if (hitInfo.collider.name == "HabitatLightsButton")
                        {
                            SurvivalTimer.Instance.CurrentHabitat.PlayerToggleLights();
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
                    newPrompt = OnFoodHover(hitInfo.collider, doInteract, Prompts.MealOrganicEatHint, Matter.OrganicMeal);
                }
                else if (hitInfo.collider.CompareTag("mealprepared"))
                {
                    newPrompt = OnFoodHover(hitInfo.collider, doInteract, Prompts.MealPreparedEatHint, Matter.RationMeal);
                }
                else if (hitInfo.collider.CompareTag("mealshake"))
                {
                    newPrompt = OnFoodHover(hitInfo.collider, doInteract, Prompts.MealShakeEatHint, Matter.MealShake);
                }
                else if (hitInfo.collider.CompareTag("terminal"))
                {
                    if (doInteract)
                    {
                        CurrentTerminal = hitInfo.collider.GetComponent<Terminal>();
                        if (CurrentTerminal == null)
                            CurrentTerminal = hitInfo.collider.transform.parent.GetChild(0).GetChild(0).GetComponent<Terminal>();

                        BeginTerminal();
                    }
                    else
                    {
                        newPrompt = Prompts.TerminalEnter;
                    }
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
                else if (hitInfo.collider.CompareTag("workshop"))
                {
                    if (doInteract)
                    {
                        Workshop w = hitInfo.collider.transform.root.GetComponent<Workshop>();
                        if (w != null)
                        {
                            CurrentCraftablePlanner = w;

                            this.ToggleCraftableBlueprintMode(true);

                            if (CurrentCraftablePlanner.CurrentCraftable != Craftable.Unspecified)
                            {
                                this.CurrentMode = InputMode.Crafting;
                            }
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.WorkshopHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("locker"))
                {
                    Workshop w = hitInfo.collider.transform.root.GetComponent<Workshop>();
                    if (w != null)
                    {
                        if (doInteract)
                        {
                            w.SwapEquipment(hitInfo.collider.transform);
                        }
                        else
                        {
                            newPrompt = w.GetLockerPrompt(hitInfo.collider.transform);
                        }
                    }
                }
                else if (hitInfo.collider.CompareTag("suit"))
                {
                    if (doInteract)
                    {

                    }
                    else
                    {
                        newPrompt = Prompts.WorkshopSuitHint;
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
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        CurrentEVAStation = hitInfo.collider.transform.root.GetComponent<EVAStation>();

                        if (CurrentEVAStation != null)
                            CurrentEVAStation.ToggleUse(true);
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
                else if (hitInfo.collider.CompareTag("toggle"))
                {
                    if (doInteract)
                    {
                        IToggleReceiver @interface = null;
                        if (ToggleMap.ToggleLookup.TryGetValue(hitInfo.collider.transform, out @interface))
                        {
                            @interface.Toggle(hitInfo.collider.transform);
                        }
                    }

                    newPrompt = Prompts.ToggleHint;
                }
                else if (hitInfo.collider.CompareTag("pumpHandle"))
                {
                    Pump pump = hitInfo.collider.transform.parent.parent.GetComponent<Pump>();

                    if (pump != null)
                    {
                        if (pump.PumpMode && Input.GetMouseButtonUp(0))
                        {
                            pump.StartPumpingIn();
                            PlayInteractionClip(hitInfo.point, pump.HandleChangeClip);
                        }
                        else if (pump.PumpMode && Input.GetMouseButtonUp(1))
                        {
                            pump.StartPumpingOut();
                            PlayInteractionClip(hitInfo.point, pump.HandleChangeClip);
                        }
                        else if (pump.ValveMode && doInteract)
                        {
                            pump.ToggleValve();
                            PlayInteractionClip(hitInfo.point, pump.HandleChangeClip);
                        }
                        else
                        {
                            newPrompt = pump.CurrentPromptBasedOnPumpStatus;
                        }
                    }
                    else
                    {
                        IceDrill drill = hitInfo.collider.transform.root.GetComponent<IceDrill>();

                        if (drill != null)
                        {
                            if (doInteract)
                            {
                                drill.ToggleDrilling();
                            }
                            else
                            {
                                newPrompt = Prompts.StartDrillHint;
                            }
                        }
                    }

                }
                else if (hitInfo.collider.CompareTag("powerSwitch"))
                {
                    if (doInteract)
                    {
                        //PlayInteractionClip(hitInfo.point, storage.HandleChangeClip);
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
                else if (hitInfo.collider.CompareTag("harvestable"))
                {
                    IHarvestable harvestable = hitInfo.collider.transform.root.GetComponent<IHarvestable>();

                    if (harvestable.CanHarvest)
                    {
                        if (Input.GetKey(KeyCode.E))
                        {
                            harvestable.Harvest(Time.deltaTime);
                        }

                        Prompts.HarvestHint.Progress = harvestable.HarvestProgress;

                        newPrompt = Prompts.HarvestHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("deposit"))
                {
                    Deposit deposit = hitInfo.collider.GetComponent<Deposit>();

                    newPrompt = Prompts.DepositHint;
                    newPrompt.Progress = deposit.Data.Extractable.UtilizationPercentage;
                    newPrompt.Description = deposit.Data.ExtractableHint;
                }
                else if (hitInfo.collider.CompareTag("corridor") && SurvivalTimer.Instance.IsNotInHabitat)
                {
                    Powerline powerline = hitInfo.collider.transform.parent.GetComponent<Powerline>();

                    if (powerline != null)
                    {
                        if (doInteract)
                        {
                            powerline.Remove();
                        }
                        else
                        {
                            newPrompt = Prompts.CorridorDeconstructHint;
                        }
                    }
                }
            }
            else if (doInteract)
            {
                if (selectedBulkhead != null)
                {
                    selectedBulkhead = null;
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
        else if (CurrentEVAStation != null)
        {
            CurrentEVAStation.ToggleUse(false);
            CurrentEVAStation = null;
        }

        if (!doInteract && Input.GetKeyUp(KeyCode.P))
        {
            PlacePostIt();
        }
    }
    
    internal void ToggleRepairMode(bool isRepairMode)
    {
        Loadout.ToggleRepairWrench(isRepairMode);
        Equip(Slot.PrimaryTool);
    }

    internal void ToggleMenu()
    {
        CurrentMode = CurrentMode == InputMode.Menu ? InputMode.Normal : InputMode.Menu;
        GuiBridge.Instance._ToggleEscapeMenuProgrammatically();
        FPSController.FreezeLook = CurrentMode == InputMode.Menu;
        Time.timeScale = CurrentMode == InputMode.Menu ? 0 : 1f;
    }

    public void PlayInteractionClip(Vector3 point, AudioClip handleChangeClip, bool oneShot = true)
    {
        this.InteractionSource.transform.position = point;
        if (oneShot)
            this.InteractionSource.PlayOneShot(handleChangeClip);
        else
        {
            this.InteractionSource.clip = handleChangeClip;
            this.InteractionSource.loop = true;
            this.InteractionSource.Play();
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
            FPSController.FreezeMovement = true;
            this.RefreshEquipmentState();
        }
    }

    private PromptInfo OnExistingPipe(bool doInteract, RaycastHit hitInfo)
    {
        if (doInteract)
        {
            //pipe script is on parent object
            Pipe pipeScript = hitInfo.collider.transform.parent.GetComponent<Pipe>();
            ModuleGameplay from = pipeScript.Data.From;
            ModuleGameplay to = pipeScript.Data.To;

            if (from == null || to == null)
            {
                UnityEngine.Debug.LogWarning("Pipe not connected to two modules!");
            }
            else
            {
                IndustryExtensions.RemoveAdjacentPumpable(from, to);
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

    private PromptInfo OnExistingPowerline(bool doInteract, RaycastHit hitInfo)
    {
        if (doInteract)
        {
            Powerline powerline = hitInfo.collider.transform.GetComponent<Powerline>();
            powerline.Remove();
            return null;
        }
        else
        {
            return Prompts.ExistingPowerlineRemovalHint;
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
        else
        {
            if (Input.GetKeyUp(KeyCode.G))
            {
                ToggleModuleBlueprintMode(true);
            }
            else if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleModuleBlueprintMode(false);
                ModulePlan.Reset();
            }
        }        
    }

    private void ToggleModuleBlueprintMode(bool showBlueprint)
    {
        FPSController.FreezeLook = showBlueprint;
        FloorplanBridge.Instance.ToggleModulePanel(showBlueprint);
    }

    private void ToggleCraftableBlueprintMode(bool showBlueprint)
    {
        FPSController.FreezeLook = showBlueprint;
        CurrentCraftablePlanner.ToggleCraftableView(showBlueprint);
    }

    private bool CastRay(out RaycastHit hitInfo, QueryTriggerInteraction triggerInteraction, params string[] layerNames)
    {
        return Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, InteractionRaycastDistance, LayerMask.GetMask(layerNames), triggerInteraction);
    }

    private PromptInfo OnFoodHover(Collider collider, bool doInteract, PromptInfo eatHint, Matter mealType)
    {
        if (doInteract)
        {
            collider.transform.root.GetComponent<Habitat>().Eat(mealType);
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
        return OnLinkable(doInteract, hitInfo, selectedPowerSocket, value => {
            selectedPowerSocket = value;

            if (value != null)
            {
                CurrentMode = InputMode.Powerline;
                GuiBridge.Instance.RefreshMode();
            }
        }, PlacePowerPlug, Prompts.PowerPlugPrompts);
    }

    private PromptInfo OnBulkhead(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedBulkhead, value => {
            selectedBulkhead = value;

        }, PlaceTube, Prompts.BulkheadBridgePrompts);
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
                selectedCompound = Matter.Unspecified;
            else {
                selectedCompound = other;
                CurrentMode = InputMode.Pipeline;
                GuiBridge.Instance.RefreshMode();
            }

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
        Transform zoneT = (Transform)GameObject.Instantiate(ModuleBridge.Instance.ConstructionZonePrefab, ModulePlan.Visualization.position, ModulePlan.Visualization.rotation);

        ConstructionZone zone = zoneT.GetComponent<ConstructionZone>();

        zone.Initialize(ModulePlan.Type);

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt))
            zone.Complete();

        ModulePlan.Reset();
        Equip(Slot.Unequipped);
    }

    private void Equip(Slot s)
    {
        Slot lastActive = Loadout.ActiveSlot;
        if (Loadout[s] == Equipment.Locked)
            s = Slot.Unequipped;

        Loadout.ActiveSlot = s;

        RefreshEquipmentState(lastActive);
    }

    private void CommonInteriorEquipmentState()
    {
        this.FPSController.FreezeLook = true;
        AlternativeCamera.cullingMask = 1 << FloorplanLayerIndex;
        AlternativeCamera.enabled = true;
    }

    private void RefreshEquipmentState(Slot? lastActive = null)
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
                ToggleModuleBlueprintMode(true);
                break;
            case Equipment.Screwdriver:
                this.CommonInteriorEquipmentState();
                FloorplanBridge.Instance.ToggleStuffPanel(true);
                break;
            case Equipment.Wheelbarrow:
                this.CommonInteriorEquipmentState();
                FloorplanBridge.Instance.ToggleFloorplanPanel(true);
                break;
            default:
                AlternativeCamera.enabled = false;
                break;
        }

        //if we're switching from one to another
        //we want to do cleanup on objects
        if (lastActive.HasValue && lastActive.Value != Loadout.ActiveSlot)
        {
            switch (Loadout[lastActive.Value])
            {
                case Equipment.Blueprints:
                    ModulePlan.Reset();
                    break;
                case Equipment.Wheelbarrow:
                    FloorPlan.Reset();
                    break;
                case Equipment.Screwdriver:
                    StuffPlan.Reset();
                    break;
            }
        }
        GuiBridge.Instance.RefreshMode();
    }

    private float oldMass;
    internal void PickUpObject(Rigidbody rigid, IMovableSnappable snappable)
    {
        if (snappable != null && snappable.IsSnapped)
        {
            snappable.UnsnapCrate();
        }

        carriedObject = rigid;
        carriedObject.useGravity = false;
        //carriedObject.isKinematic = true;
        oldMass = rigid.mass;
        carriedObject.mass = 0f;
        carriedObject.velocity = Vector3.zero;
        carriedObject.transform.SetParent(this.transform);
        snappable.OnPickedUp();
    }

    internal void DropObject()
    {
        if (carriedObject != null)
        {
            //carriedObject.isKinematic = false;
            carriedObject.transform.SetParent(null);
            carriedObject.useGravity = true;
            carriedObject.mass = oldMass;
        }

        carriedObject = null;
    }

    private void ToggleVehicle(RoverInput roverInput)
    {
        //exiting vehicle
        if (roverInput == null && DrivingRoverInput != null)
        {
            IsOnFoot = true;
            DrivingRoverInput.AcceptInput = false;
            DrivingRoverInput.ExitBrake();
            FPSController.transform.position = DrivingRoverInput.transform.Find("Exit").transform.position;
            FPSController.transform.SetParent(null);
            FPSController.CharacterController.enabled = true;
            FPSController.FreezeMovement = false;
            DrivingRoverInput = null;
        }
        else //entering vehicle
        {
            IsOnFoot = false;
            Headlamp1.enabled = Headlamp2.enabled = false;
            //FPSController.enabled = false;
            DrivingRoverInput = roverInput;
            DrivingRoverInput.AcceptInput = true;
            if (DrivingRoverInput.Data.HatchOpen)
                DrivingRoverInput.ToggleHatchback(false);
            FPSController.transform.SetParent(DrivingRoverInput.transform.Find("Enter").transform);
            FPSController.transform.localPosition = Vector3.zero;
            FPSController.transform.localRotation = Quaternion.identity;
            FPSController.InitializeMouseLook();
            FPSController.FreezeMovement = true;
            FPSController.CharacterController.enabled = false;
        }

        SurvivalTimer.Instance.RefreshResources(IsInVehicle, DrivingRoverInput);
        GuiBridge.Instance.RefreshSurvivalPanel(IsInVehicle);
    }

    private void PlaceTube(Collider toBulkhead)
    {
        Transform newCorridorParent = PlaceRuntimeLinkingObject(selectedBulkhead, toBulkhead, tubePrefab, createdTubes, hideObjectEnds: true, setScale: false);
        Transform newCorridor = newCorridorParent.GetChild(0);
        MeshFilter newCorridorFilter = newCorridor.GetComponent<MeshFilter>();

        //create the interior mesh
        Mesh baseCorridorMesh = newCorridorFilter.sharedMesh;
        Mesh newCorridorMesh = (Mesh)Instantiate(baseCorridorMesh);

        Transform anchorT1 = selectedBulkhead.transform.parent;
        Mesh anchorM1 = anchorT1.GetComponent<MeshFilter>().mesh;

        Transform anchorT2 = toBulkhead.transform.parent;
        Mesh anchorM2 = anchorT2.GetComponent<MeshFilter>().mesh;

        //modify the ends of the mesh
        Construction.SetCorridorVertices(newCorridor, newCorridorMesh, anchorT1, anchorM1, anchorT2, anchorM2);

        //assign the programmatically created mesh to the mesh filter
        newCorridorFilter.mesh = newCorridorMesh;
        //and tell the mesh collider to use this new mesh as well
        newCorridor.GetComponent<MeshCollider>().sharedMesh = newCorridorMesh;

        IHabitatModule habMod1 = anchorT1.root.GetComponent<IHabitatModule>();
        IHabitatModule habMod2 = anchorT2.root.GetComponent<IHabitatModule>();

        Powerline powerline = newCorridorParent.GetComponent<Powerline>();
        powerline.AssignConnections(habMod1, habMod2, selectedBulkhead.transform.parent, toBulkhead.transform.parent);
    }

    private void PlaceGasPipe(Collider collider)
    {
        Transform newPipeTransform = PlaceRuntimeLinkingObject(selectedGasValve, collider, gasPipePrefab, createdPipes);

        if (selectedCompound == Matter.Unspecified)
            selectedCompound = GetCompoundFromValve(collider);

        ModuleGameplay g1 = selectedGasValve.transform.root.GetComponent<ModuleGameplay>(), g2 = collider.transform.root.GetComponent<ModuleGameplay>();
        if (g1 != null && g2 != null)
        {
            newPipeTransform.GetComponent<Pipe>().AssignConnections(selectedCompound, g1, g2);

            selectedCompound = Matter.Unspecified;

            CurrentMode = InputMode.Normal;
            GuiBridge.Instance.RefreshMode();
        }
    }

    private void PlacePowerPlug(Collider collider)
    {
        Transform power = PlaceRuntimeLinkingObject(selectedPowerSocket, collider, powerlinePrefab, createdPowerlines);
        
        IPowerable g1 = selectedPowerSocket.transform.root.GetComponent<IPowerable>(), g2 = collider.transform.root.GetComponent<IPowerable>();
        if (g1 != null && g2 != null && g1 != g2)
        {
            power.GetComponent<Powerline>().AssignConnections(g1, g2, selectedPowerSocket.transform, collider.transform);
        }

        CurrentMode = InputMode.Normal;
        GuiBridge.Instance.RefreshMode();
    }

    private static Transform PlaceRuntimeLinkingObject(
        Collider firstObject, 
        Collider otherObject, 
        Transform linkingObjectPrefab, List<Transform> addToList, 
        bool hideObjectEnds = false, 
        float extraScale = 0f,
        bool setScale = true)
    {
        float distanceBetween = Vector3.Distance(firstObject.transform.position, otherObject.transform.position);

        Vector3 midpoint = Vector3.Lerp(firstObject.transform.position, otherObject.transform.position, 0.5f);
        Transform newObj = GameObject.Instantiate<Transform>(linkingObjectPrefab);

        newObj.position = midpoint;
        newObj.LookAt(otherObject.transform);

        if (setScale)
            newObj.localScale = new Vector3(newObj.localScale.x, newObj.localScale.y, (distanceBetween / 2f) + extraScale);

        addToList.Add(newObj);

        if (hideObjectEnds)
        {
            firstObject.gameObject.SetActive(false);
            otherObject.gameObject.SetActive(false);
        }

        return newObj;
    }

    internal void PlanModule(Module planModule)
    {
        this.ModulePlan.SetVisualization(planModule);
    }

    internal void PlanStuff(Stuff s)
    {
        this.StuffPlan.SetVisualization(s);
    }

    internal void PlanFloor(Floorplan whatToBuild)
    {
        this.FloorPlan.SetVisualization(whatToBuild);
    }

    public void KillPlayer(string reason)
    {
        GuiBridge.Instance.ShowKillMenu(reason);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Time.timeScale = 0f;
        FPSController.enabled = false;
        this.enabled = false;
    }

    #region sleep mechanic
    private PlayerLerpContext lerpCtx;

    private struct PlayerLerpContext
    {
        public Vector3 FromPosition, ToPosition, ExitPosition;
        public Quaternion FromRotation, ToRotation, ExitRotation;
        public float Duration;
        private float Time;
        public bool Done;
        public bool RotateCam;

        public void StandUp()
        {
            FromPosition = ToPosition;
            FromRotation = ToRotation;
            ToPosition = ExitPosition;
            ToRotation = ExitRotation;
            Done = false;
            Time = 0f;
        }

        public void Tick(Transform body, Transform camera)
        {
            this.Time += UnityEngine.Time.deltaTime;
            if (this.Time > this.Duration)
            {
                body.position = ToPosition;

                if (RotateCam)
                    camera.rotation = ToRotation;

                Done = true;
            }
            else
            {
                body.position = Vector3.Lerp(FromPosition, ToPosition, Time / Duration);

                if (RotateCam)
                    camera.rotation = Quaternion.Lerp(FromRotation, ToRotation, Time / Duration);
            }
        }
    }

    private void BeginSleep(Transform enterTranform, Transform exitTransform)
    {
        lerpCtx = new PlayerLerpContext()
        {
            FromPosition = FPSController.transform.position,
            ToPosition = enterTranform.position,
            FromRotation = Camera.main.transform.rotation,
            ToRotation = enterTranform.rotation,
            Duration = .5f,
            ExitPosition = exitTransform.position,
            ExitRotation = exitTransform.rotation
        };

        ToggleSleep(true);

        StartCoroutine(LerpTick());
    }

    private void ExitSleep()
    {
        ToggleSleep(false);
    }

    private Terminal CurrentTerminal;
    private void BeginTerminal()
    {
        lerpCtx = new PlayerLerpContext()
        {
            FromPosition = FPSController.transform.position,
            ToPosition = CurrentTerminal.transform.position + CurrentTerminal.transform.TransformDirection(Vector3.back) + (Vector3.down * 0.8f),
            FromRotation = Camera.main.transform.rotation,
            ToRotation = Quaternion.LookRotation(CurrentTerminal.transform.TransformDirection(Vector3.forward), CurrentTerminal.transform.TransformDirection(Vector3.up)),
            Duration = .5f,
            ExitPosition = FPSController.transform.position,
            ExitRotation = FPSController.transform.rotation,
            RotateCam = true
        };

        ToggleTerminal(true);

        StartCoroutine(LerpTick());
    }

    private void ExitTerminal()
    {
        ToggleTerminal(false);
    }

    private void ToggleTerminal(bool inTerminal)
    {
        this.CurrentMode = inTerminal ? InputMode.Terminal : InputMode.Normal;

        FPSController.FreezeMovement = inTerminal;
        FPSController.FreezeLook = inTerminal;
        Cursor.visible = inTerminal;
        Cursor.lockState = CursorLockMode.None;
        GuiBridge.Instance.Crosshair.gameObject.SetActive(!inTerminal);

        this.RefreshEquipmentState();
    }

    private void ToggleSleep(bool isAsleep)
    {
        this.CurrentMode = isAsleep ? InputMode.Sleep : InputMode.Normal;

        FPSController.FreezeMovement = isAsleep;

        this.RefreshEquipmentState();
    }

    internal enum WakeSignal { PlayerCancel, ResourceRequired, DayStart }
    internal WakeSignal? wakeyWakeySignal = null;
    private EVAStation CurrentEVAStation;

    private void HandleSleepInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Comma))
        {
            SunOrbit.Instance.SlowDown();
        }
        else if (Input.GetKeyUp(KeyCode.Period))
        {
            SunOrbit.Instance.SpeedUp();
        } else if (doInteract)
        {
            if (SunOrbit.Instance.RunTilMorning)
            {
                SunOrbit.Instance.ToggleSleepUntilMorning(false, WakeSignal.PlayerCancel);
            }
            else
            {
                wakeyWakeySignal = WakeSignal.PlayerCancel;
            }
        }

        if (wakeyWakeySignal.HasValue)
        {
            lerpCtx.StandUp(); //reset ctx
            StartCoroutine(LerpTick(ExitSleep));

            if (wakeyWakeySignal.Value == WakeSignal.DayStart)
            {
                GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.GoodMorningHomesteader);
            }
            wakeyWakeySignal = null;
        }
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            SunOrbit.Instance.ToggleSleepUntilMorning(true);
        }
        else
        {
            if (SunOrbit.Instance.RunTilMorning)
            {
                newPrompt = Prompts.SleepTilMorningExitHint;
            }
            else
            {
                newPrompt = Prompts.BedExitHint;
            }
        }
    }

    private void HandleTerminalInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            lerpCtx.StandUp(); //reset ctx
            StartCoroutine(LerpTick(ExitTerminal));
        }
    }

    private IEnumerator LerpTick(Action onDone = null)
    {
        while (!lerpCtx.Done)
        {
            lerpCtx.Tick(FPSController.transform, Camera.main.transform);

            //every time we modify the camera via script we need to do this call
            FPSController.InitializeMouseLook();

            yield return null;
        }

        if (onDone != null)
        {
            onDone();
        }
    }
    #endregion
}
