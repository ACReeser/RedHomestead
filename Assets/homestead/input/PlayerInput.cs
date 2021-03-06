﻿using UnityEngine;
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
    public AudioClip Drill, Construction, PlugIn, DoorOpen, DoorClose, DoorSmallOpen, DoorSmallClose, EatCrispyFood, DrinkShake, DrinkWater, StartSleep, Repair;
}
[Serializable]
public struct HeartbeatVocalNoiseClips
{
    public AudioClip FastHeartbeat, SlowHeartbeat, MediumHeartbeat, SlowToDeathHeartbeat, Chattering, Gasping;
}

/// <summary>
/// Responsible for raycasting, modes, and gameplay input
/// </summary>
public class PlayerInput : MonoBehaviour {
    public static PlayerInput Instance;

    public enum InputMode { Menu = -1, Normal = 0, PostIt, Sleep, Terminal, Pipeline, Powerline, Umbilical, ThinkingAboutCrafting, Crafting, Printing }

    private const float InteractionRaycastDistance = 10f;
    private const int ChemicalFlowLayerIndex = 9;
    private const int FloorplanLayerIndex = 10;
    private const int ElectricityLayerIndex = 11;
    private const int RadarLayerIndex = 12;

#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
    public static bool DoNotDisturb;
#endif

    public Camera AlternativeCamera;
    public Light Headlamp1, Headlamp2;
    /// <summary>
    /// Tube prefab to be created when linking bulkheads
    /// </summary>
    public Transform tubePrefab, gasPipePrefab, powerlinePrefab, umbilicalPrefab;
    /// <summary>
    /// the FPS input script (usually on the parent transform)
    /// </summary>
    public CustomFPSController FPSController;
    public Transform[] ScienceExperimentMarkers;

    /// <summary>
    /// The prefab for a construction zone
    /// </summary>
    public Transform PostItNotePrefab;

    /// <summary>
    /// Array of tool prefabs indexed on Equipment enum
    /// </summary>
    public Transform[] ToolPrefabs;
    /// <summary>
    /// the material to put on module prefabs
    /// when planning where to put them on the ground
    /// </summary>
    public Material translucentPlanningMat, translucentInvalidPlanningMat;
    public ParticleSystem DrillSparks, Blower;

    public AudioSource InteractionSource, HeartbeatSource, VocalSource;
    public InteractionClips Sfx;
    public HeartbeatVocalNoiseClips HeartbeatsAndVocals;
    public AudioClip GoodMorningHomesteader;

    internal InputMode CurrentMode = InputMode.Normal;
    internal Loadout Loadout;
    internal bool LookForRepairs = false;

    internal void SetPressure(bool pressurized)
    {
        FPSController.PlaceBootprints = !pressurized;
    }

    private RoverInput DrivingRoverInput;
    internal Collider selectedBulkhead { get; private set; }
    internal Collider selectedGasValve { get; private set; }
    internal Collider selectedPowerSocket { get; private set; }
    private Rigidbody carriedObject;
    private Matter selectedCompound = Matter.Unspecified;
    private List<Transform> createdTubes = new List<Transform>();
    private List<Transform> createdPipes = new List<Transform>();
    private List<Transform> createdUmbilicals = new List<Transform>();
    private List<Transform> createdPowerlines = new List<Transform>();
    internal bool IsOnFoot { get; private set; }
    internal bool IsInSuit { get; private set; }
    private bool reportMenuOpen = false;
    internal ParticleSystem DrillingParticles;

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
    public Planning<Module> ModulePlan { get; private set; }
    private Planning<Stuff> StuffPlan = new Planning<Stuff>();
    private Planning<Floorplan> FloorPlan = new Planning<Floorplan>();

    private Transform lastHobbitHoleT, lastDepositT;
    private HobbitHole lastHobbitHole;
    private Deposit lastDeposit;
    private ThreeDPrinter lastPrinter;

    void Awake()
    {
        Instance = this;
        ModulePlan = new Planning<Module>();
        InteractionSource.transform.SetParent(null);
        IsOnFoot = true;
        foreach(Transform t in this.ScienceExperimentMarkers)
        {
            t.gameObject.SetActive(false);
        }
    }

    public void Start()
    {
        Loadout = new Loadout();
        GuiBridge.Instance.BuildRadialMenu(this.Loadout);
        Equip(Slot.Unequipped);
        PrefabCacheUtils.TranslucentValidPlanningMaterial = translucentPlanningMat;
        PrefabCacheUtils.TranslucentInvalidPlanningMaterial = translucentInvalidPlanningMat;
        Autosave.Instance.AutosaveEnabled = true;
        DrillSparks.transform.SetParent(null);
        GuiBridge.Instance.RefreshSurvivalPanel(false, false);
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
        if (Input.GetKeyUp(KeyCode.End) && Input.GetKey(KeyCode.LeftShift)) {
            DoNotDisturb = !DoNotDisturb;
            Debug.LogWarning("DoNotDisturb=" + DoNotDisturb);
        }
        if (Input.GetKeyUp(KeyCode.End) && Input.GetKey(KeyCode.RightControl))
        {
            Gremlin.Instance.TriggerFailure();
        }
        if (Input.GetKeyUp(KeyCode.Home) && Input.GetKey(KeyCode.RightControl))
        {
            Gremlin.Instance.TriggerRepair();
        }

        if (Input.GetKeyUp(KeyCode.Keypad0) && Input.GetKey(KeyCode.RightControl) && CargoLander.Instance != null)
        {
            if (CargoLander.Instance.Data.State == CargoLander.FlightState.Disabled)
                CargoLander.Instance.Land();
            else if (CargoLander.Instance.Data.State == CargoLander.FlightState.Landed)
                CargoLander.Instance.TakeOff();
        }
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
                    case Equipment.Sledge:
                        HandleSledgeInput(ref newPrompt);
                        break;
                }
                break;
            case InputMode.PostIt:
                HandlePostItInput(ref newPrompt, doInteract);
                break;
            case InputMode.Sleep:
                HandleSleepInput(ref newPrompt, doInteract);
                break;
            case InputMode.ThinkingAboutCrafting:
                if (Input.GetKeyUp(KeyCode.G))
                {
                    ToggleCraftableBlueprintMode(true);
                }
                else if (Input.GetKeyUp(KeyCode.Escape))
                {
                    ToggleCraftableBlueprintMode(false);
                    CurrentMode = InputMode.Normal;
                }
                break;
            case InputMode.Crafting:
                HandleCraftingInput(ref newPrompt, doInteract);
                break;
            case InputMode.Printing:
                HandlePrintingInput(ref newPrompt, doInteract);
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
            case InputMode.Umbilical:
                HandleUmbilicalInput(ref newPrompt, doInteract);
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

    private void HandlePrintingInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            CurrentMode = InputMode.Normal;
            TogglePrintableBlueprintMode(false);
            SunOrbit.Instance.ResetToNormalTime();
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.Tab))
                GuiBridge.Instance.Printer.ToggleAvailable();
            else if (Input.GetKeyUp(KeyCode.Comma))
            {
                SunOrbit.Instance.SlowDown();
            }
            else if (Input.GetKeyUp(KeyCode.Period))
            {
                SunOrbit.Instance.SpeedUp();
            }
            else if (Input.GetKeyUp(KeyCode.X))
            {
                lastPrinter.Scrap();
                CurrentMode = InputMode.Normal;
                TogglePrintableBlueprintMode(false);
                SunOrbit.Instance.ResetToNormalTime();
            }

            GuiBridge.Instance.Printer.RefreshDetail();
        }
    }

    private void HandleSledgeInput(ref PromptInfo newPrompt)
    {
        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Ignore, layerNames: "Default"))
        {
            if (hitInfo.collider != null)
            {
                ModuleGameplay module = hitInfo.collider.transform.root.GetComponent<ModuleGameplay>();

                if (module != null)
                {
                    if (FlowManager.Instance.PowerGrids.Edges.ContainsKey(module) || (module.AdjacentPumpables != null && module.AdjacentPumpables.Count > 0))
                    {
                        newPrompt = Prompts.SledgehammerConnectedHint;
                    }
                    else
                    {
                        if (Input.GetKeyUp(KeyCode.X))
                        {
                            GameObject.Destroy(module.transform.root.gameObject);
                        }
                        else
                        {
                            newPrompt = Prompts.SledgehammerHint;
                        }
                    }
                }
            }
        }
    }

    private void HandleUmbilicalInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            selectedUmbilical = null;
            CurrentMode = InputMode.Normal;
            GuiBridge.Instance.RefreshMode();
        }

        RaycastHit hitInfo;
        if (CastRay(out hitInfo, QueryTriggerInteraction.Collide, layerNames: "interaction"))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("umbilicalPlug"))
                {
                    newPrompt = OnUmbilical(newPrompt, doInteract, hitInfo);
                }
                else
                {
                    newPrompt = Prompts.StopUmbilicalHint;
                }
            }
            else
            {
                newPrompt = Prompts.StopUmbilicalHint;
            }
        }
        else
        {
            newPrompt = Prompts.StopUmbilicalHint;
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
        else if (Input.GetKeyUp(KeyCode.X))
        {
            wakeyWakeySignal = WakeSignal.PlayerCancel;
            CurrentCraftablePlanner.SetCurrentCraftable(Craftable.Unspecified);
        }

        if (CurrentCraftablePlanner != null)
        {
            CurrentCraftablePlanner.MakeProgress(Time.deltaTime);
        }

        if (wakeyWakeySignal.HasValue && wakeyWakeySignal.Value != WakeSignal.DayStart)
        {
            SunOrbit.Instance.ResetToNormalTime();
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
                        if (Input.GetMouseButtonDown(0))
                            PlayInteractionClip(repairable.transform.position, Sfx.Repair, volumeScale: .5f);

                        if (Input.GetMouseButton(0))
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

    private const float DefaultManualMiningPerTick = 0.004f;

    private void HandleDefaultInput(ref PromptInfo newPrompt, bool doInteract)
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (GuiBridge.Instance.PowerGrid.IsOpen)
            {
                GuiBridge.Instance.PowerGrid.Toggle();
            }
            else if (reportMenuOpen)
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
            if (ModulePlan.IsActive)
            {
                Equip(Slot.Unequipped);
                ToggleModuleBlueprintMode(false);
            }

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

        if (Input.GetKeyUp(KeyCode.P) && SurvivalTimer.Instance.CurrentHabitat != null && SurvivalTimer.Instance.CurrentHabitat.PowerGridInstanceID != null)
        {
            GuiBridge.Instance.PowerGrid.Render(FlowManager.Instance.PowerGrids[SurvivalTimer.Instance.CurrentHabitat.PowerGridInstanceID]);
            GuiBridge.Instance.PowerGrid.Toggle();
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
                            if (res != null)
                            {
                                newPrompt.ItalicizedText = res.GetText();
                                newPrompt.UsesProgress = !(res.Progress < 0f);
                                newPrompt.Progress = res.Progress;
                            }
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
                            {
                                newPrompt.ItalicizedText = res.GetText();
                                newPrompt.UsesProgress = !(res.Progress < 0f);
                                newPrompt.Progress = res.Progress;
                            }
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
                else if (hitInfo.collider.gameObject.CompareTag("umbilicalPlug"))
                {
                    newPrompt = OnUmbilical(newPrompt, doInteract, hitInfo);
                }
                else if (hitInfo.collider.gameObject.CompareTag("pipe"))
                {
                    newPrompt = OnExistingPipe(doInteract, hitInfo);
                }
                else if (hitInfo.collider.gameObject.CompareTag("powerline"))
                {
                    newPrompt = OnExistingPowerline(doInteract, hitInfo);
                }
                else if (hitInfo.collider.gameObject.CompareTag("umbilical"))
                {
                    newPrompt = OnExistingUmbilical(doInteract, hitInfo);
                }
                else if (IsOnFoot && hitInfo.collider.gameObject.CompareTag("rover"))
                {
                    RoverInput ri = hitInfo.collider.transform.GetComponent<RoverInput>();

                    if (doInteract)
                        ToggleVehicle(ri);
                    else
                        newPrompt = Prompts.DriveRoverPrompt;
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
                                    PlayInteractionClip(zone.transform.position, Sfx.Construction, volumeScale: .5f);

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
                            {
                                PlayInteractionClip(hitInfo.point, doorM.DoorType == DoorType.Large ? Sfx.DoorClose : Sfx.DoorSmallClose, volumeScale: .4f);
                                doorM.ToggleDoor(hitInfo.collider.transform);
                            }
                            else
                                newPrompt = Prompts.CloseDoorHint;
                            break;
                        case Airlock.ClosedDoorName:
                        default:
                            if (doInteract)
                            {
                                PlayInteractionClip(hitInfo.point, doorM.DoorType == DoorType.Large ? Sfx.DoorOpen : Sfx.DoorSmallOpen, volumeScale:.4f);
                                doorM.ToggleDoor(hitInfo.collider.transform);
                            }
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
                        if (hitInfo.collider.transform.parent != lastHobbitHoleT)
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
                else if (hitInfo.collider.CompareTag("deposit"))
                {
                    if (hitInfo.collider.transform != lastDepositT)
                    {
                        lastDeposit = hitInfo.collider.transform.GetComponent<Deposit>();
                    }

                    if (lastDeposit != null)
                    {
                        if (Loadout.Equipped == Equipment.Sampler)
                        {
                            if (Input.GetMouseButtonDown(0))
                            {
                                bool foundLab = false;
                                foreach(var lab in ScienceLab.ActiveLabs)
                                {
                                    if (lab.FlexData.CurrentGeoExperiment != null && lab.FlexData.CurrentGeoExperiment.DepositID == lastDeposit.Data.DepositInstanceID)
                                    {
                                        foundLab = true;
                                        lab.OnGeologySampleTaken(lastDeposit);
                                        break;
                                    }
                                }
                                if (foundLab)
                                {
                                    GuiBridge.Instance.ShowNews(NewsSource.SampleTaken);
                                }
                            }
                            else
                            {
                                newPrompt = Prompts.DepositSampleHint;
                                newPrompt.Progress = lastDeposit.Data.Extractable.UtilizationPercentage;
                            }
                        }
                        else if (Loadout.Equipped == Equipment.RockDrill && lastDeposit.HasCrate)
                        {
                            if (Input.GetMouseButtonDown(0))
                            {
                                lastDeposit.ToggleMining(true);
                                PlayInteractionClip(hitInfo.collider.transform.position, Sfx.Drill, false);
                                DrillSparks.Play();
                            }

                            if (Input.GetMouseButton(0))
                            {
                                newPrompt = Prompts.MineHint;
                                newPrompt.Progress = lastDeposit.Mine(DefaultManualMiningPerTick * Time.deltaTime * PerkMultipliers.ExcavationSpeed);

                                if (Prompts.MineHint.Progress >= 1f)
                                {
                                    StopDrilling();
                                }
                                else
                                {
                                    DrillSparks.transform.position = hitInfo.point + hitInfo.normal.normalized * .02f;
                                }
                            }
                            else
                            {
                                StopDrilling();

                                newPrompt = Prompts.MineHint;
                                newPrompt.Progress = lastDeposit.Data.Extractable.UtilizationPercentage;
                                newPrompt.Description = lastDeposit.Data.ExtractableHint;
                                newPrompt.ItalicizedText = String.Format("{0:0}% Pure", lastDeposit.Data.Purity * 100f);
                            }
                        }
                        else
                        {
                            newPrompt = Prompts.DepositHint;
                            newPrompt.Progress = lastDeposit.Data.Extractable.UtilizationPercentage;
                            newPrompt.Description = lastDeposit.Data.ExtractableHint;
                            newPrompt.ItalicizedText = String.Format("{0:0}% Pure", lastDeposit.Data.Purity * 100f);
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.DepositHint;
                        newPrompt.Progress = lastDeposit.Data.Extractable.UtilizationPercentage;
                        newPrompt.Description = lastDeposit.Data.ExtractableHint;
                        newPrompt.ItalicizedText = String.Format("{0:0}% Pure", lastDeposit.Data.Purity * 100f);
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
                        else if (hitInfo.collider.name == "hydraulic_lever")
                        {
                            hitInfo.collider.transform.root.GetComponent<Furnace>().ToggleHydraulicLiftLever();
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.GenericButtonHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("3dprinter"))
                {
                    if (doInteract)
                    {
                        var printer = hitInfo.collider.transform.root.GetComponent<ThreeDPrinter>();
                        if (printer != null)
                        {
                            if (!printer.HasPower || !printer.IsOn)
                            {
                                GuiBridge.Instance.ShowNews(NewsSource.PrinterUnpowered);
                            }
                            else
                            {
                                CurrentMode = InputMode.Printing;
                                lastPrinter = printer;
                                TogglePrintableBlueprintMode(true, printer);
                            }
                        }
                    }
                    else
                    {
                        newPrompt = Prompts.ThreeDPrinterHint;
                    }
                }
                else if (hitInfo.collider.CompareTag("water"))
                {
                    if (doInteract)
                    {
                        PlayInteractionClip(hitInfo.collider.transform.position, Sfx.DrinkWater, true, 0.5f);
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
                    newPrompt = OnFoodHover(hitInfo.collider, doInteract, Prompts.MealOrganicEatHint, Matter.OrganicMeals, Sfx.EatCrispyFood);
                }
                else if (hitInfo.collider.CompareTag("mealprepared"))
                {
                    newPrompt = OnFoodHover(hitInfo.collider, doInteract, Prompts.MealPreparedEatHint, Matter.RationMeals, Sfx.EatCrispyFood);
                }
                else if (hitInfo.collider.CompareTag("mealshake"))
                {
                    newPrompt = OnFoodHover(hitInfo.collider, doInteract, Prompts.MealShakeEatHint, Matter.MealShakes, Sfx.DrinkShake);
                }
                else if (hitInfo.collider.CompareTag("terminal"))
                {
                    if (doInteract)
                    {
                        CurrentTerminal = hitInfo.collider.GetComponent<ITerminal>();
                        if (CurrentTerminal == null)
                        {
                            CurrentTerminal = hitInfo.collider.transform.parent.GetChild(0).GetChild(0).GetComponent<ITerminal>();
                        }

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

                            if (CurrentCraftablePlanner.CurrentCraftable == Craftable.Unspecified)
                            {
                                this.CurrentMode = InputMode.ThinkingAboutCrafting;
                            }
                            else
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
                    IEquipmentSwappable swappable = hitInfo.collider.transform.root.GetComponent<IEquipmentSwappable>();
                    if (swappable != null)
                    {
                        if (swappable.EquipmentLockers[hitInfo.collider.transform].IsGadget())
                        {
                            if (doInteract)
                            {
                                swappable.SwapEquipment(hitInfo.collider.transform, Slot.PrimaryGadget);
                            }
                            else if (Input.GetKeyUp(KeyCode.Q)) // && Game.Current.Player.PackData.HasUpgrade(RedHomestead.EVA.EVAUpgrade.Toolbelt)
                            {
                                swappable.SwapEquipment(hitInfo.collider.transform, Slot.SecondaryGadget);
                            }
                            else
                            {
                                newPrompt = swappable.GetLockerGadgetPrompt(hitInfo.collider.transform);
                            }
                        }
                        else
                        {
                            if (doInteract)
                            {
                                swappable.SwapEquipment(hitInfo.collider.transform, Slot.PrimaryTool);
                            }
                            else if (Input.GetKeyUp(KeyCode.Q) && Game.Current.Player.PackData.HasUpgrade(RedHomestead.EVA.EVAUpgrade.Toolbelt))
                            {
                                swappable.SwapEquipment(hitInfo.collider.transform, Slot.SecondaryTool);
                            }
                            else
                            {
                                newPrompt = swappable.GetLockerToolPrompt(hitInfo.collider.transform);
                            }
                        }
                    }
                }
                //else if (hitInfo.collider.CompareTag("suit"))
                //{
                //    if (doInteract)
                //    {

                //    }
                //    else
                //    {
                //        newPrompt = Prompts.WorkshopSuitHint;
                //    }
                //}
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
                    IPowerConsumerToggleable toggle = hitInfo.collider.transform.root.GetComponent<IPowerConsumerToggleable>();

                    if (doInteract)
                    {
                        //PlayInteractionClip(hitInfo.point, storage.HandleChangeClip);
                        toggle.TogglePower();
                    }
                    else if (toggle != null)
                    {
                        if (toggle.HasPower)
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
                        else
                        {
                            newPrompt = Prompts.NoPowerHint;
                        }
                    }
                }
                else if (hitInfo.collider.CompareTag("dustable"))
                {
                    if (Loadout.Equipped == Equipment.Blower)
                    {
                        if (Blower.isStopped && Input.GetMouseButtonDown(0))
                        {
                            Blower.Play();
                        }
                        else if (Blower.isPlaying && Input.GetMouseButtonUp(0))
                        {
                            Blower.Stop();
                        }

                        if (Blower.isPlaying)
                        {
                            var sp = hitInfo.collider.transform.root.GetComponent<SolarPanel>();
                            if (sp != null && sp.FlexData.DustBuildup > 0f)
                            {
                                sp.FlexData.DustBuildup -= DustRemovalPerGameSecond * Time.deltaTime;
                                sp.RefreshSolarPanelDustVisuals();
                            }
                        }
                    }
                }
                else if (hitInfo.collider.CompareTag("airbagpayload"))
                {
                    if (doInteract)
                    {
                        //bouncelander is on parent of interaction cube
                        BounceLander landerScript = hitInfo.collider.transform.root.GetComponent<BounceLander>();

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
                /*else if (hitInfo.collider.CompareTag("deposit"))
                {
                    Deposit deposit = hitInfo.collider.GetComponent<Deposit>();

                    newPrompt = Prompts.DepositHint;
                    newPrompt.Progress = deposit.Data.Extractable.UtilizationPercentage;
                    newPrompt.Description = deposit.Data.ExtractableHint;
                    //todo: assign sprite based off of deposit.Data.Container.MatterType
                }*/
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
        else if (Blower.isPlaying)
        {
            Blower.Stop();
        }
        else if (lastDeposit != null && lastDeposit.IsMining)
        {
            StopDrilling();
        }

        if (!doInteract && Input.GetKeyUp(KeyCode.N))
        {
            PlacePostIt();
        }
    }

    private void StopDrilling()
    {
        DrillSparks.Stop();
        InteractionSource.Stop();
        lastDeposit.ToggleMining(false);
    }

    private Collider selectedUmbilical;
    private PromptInfo OnUmbilical(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedUmbilical, value => {
            selectedUmbilical = value;

            if (value != null)
            {
                RoverInput.TogglePowerToUmbilical(true);
                CurrentMode = InputMode.Umbilical;
                GuiBridge.Instance.RefreshMode();
            }
        }, PlaceUmbilical, Prompts.UmbilicalPrompts);
    }

    private void PlaceUmbilical(Collider secondUmbilical)
    {
        IPowerable g1 = selectedUmbilical.transform.root.GetComponent<IPowerable>(), 
                   g2 = secondUmbilical.transform.root.GetComponent<IPowerable>();

        if (g1 != null && g2 != null && g1 != g2)
        {
            var umbilical = PlaceRuntimeLinkingObject(selectedUmbilical, secondUmbilical, umbilicalPrefab, createdUmbilicals);
            umbilical.GetComponent<Umbilical>().AssignConnections(g1, g2, selectedUmbilical.transform, secondUmbilical.transform);
        }

        selectedUmbilical = null;
        RoverInput.TogglePowerToUmbilical(false);
        CurrentMode = InputMode.Normal;
        GuiBridge.Instance.RefreshMode();
    }

    private PromptInfo OnExistingUmbilical(bool doInteract, RaycastHit hitInfo)
    {
        if (doInteract)
        {
            Umbilical umbilicalScript = hitInfo.collider.transform.root.GetComponent<Umbilical>();
            umbilicalScript.Remove();
            return null;
        }
        else
        {
            return Prompts.ExistingUmbilicalRemovalHint;
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

    public void PlayInteractionClip(Vector3 point, AudioClip handleChangeClip, bool oneShot = true, float volumeScale = 1f)
    {
        this.InteractionSource.transform.position = point;
        if (oneShot)
            this.InteractionSource.PlayOneShot(handleChangeClip, volumeScale);
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
            Pipeline pipeScript = hitInfo.collider.transform.parent.GetComponent<Pipeline>();
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
            if (Input.GetMouseButton(0) && ModulePlan.Type.CanRotate())
            {
                ModulePlan.Rotate(true);
            }
            else if (Input.GetMouseButton(1) && ModulePlan.Type.CanRotate())
            {
                ModulePlan.Rotate(false);
            }

            RaycastHit hitInfo;
            bool hit;
            bool depositSnap = false;
            if (ModulePlan.Type == Module.OreExtractor)
            {
                hit = CastRay(out hitInfo, QueryTriggerInteraction.Collide, "Default", "interaction");
                depositSnap = true;
            }
            else
            {
                hit = CastRay(out hitInfo, QueryTriggerInteraction.Ignore, "Default");
            }

            if (hit)
            {
                if (hitInfo.collider != null)
                {
                    if ((!depositSnap && hitInfo.collider.CompareTag("terrain")) || (depositSnap && hitInfo.collider.CompareTag("deposit")))
                    {
                        ModulePlan.IsValid = true;
                        //TODO: raycast 3 more times (other 3 corners)
                        //then take the average height between them
                        //and invalidate the placement if it passes some threshold
                        ModulePlan.Visualization.position = hitInfo.point;

                        if (ModulePlan.Type.IsHabitatModule())
                        {
                            GameObject[] bulkheads = GameObject.FindGameObjectsWithTag("bulkhead");

                            float close = 999f;
                            Transform closest = null;
                            foreach(var bulkhead in bulkheads)
                            {
                                if (bulkhead.transform.root == ModulePlan.Visualization.root)
                                    continue;
                                else
                                {
                                    float candidate = (bulkhead.transform.position - ModulePlan.Visualization.transform.position).sqrMagnitude;
                                    if (candidate > 36f)
                                    {
                                        continue;
                                    }
                                    else if (candidate < close)
                                    {
                                        close = candidate;
                                        closest = bulkhead.transform;
                                    }
                                }
                            }
                            if (closest != null)
                            {
                                Vector3 newPosition = closest.TransformPoint(Vector3.right * Mathf.Sqrt(close));
                                newPosition.y = hitInfo.point.y;
                                ModulePlan.Visualization.position = newPosition;

                                float rotationDegrees = Mathf.Abs( ModulePlan.Visualization.rotation.eulerAngles.y);
                                if (rotationDegrees != 0f && (rotationDegrees < 10 || rotationDegrees > 80))
                                {
                                    var vec = ModulePlan.Visualization.rotation.eulerAngles;
                                    vec.x = Mathf.Round(vec.x / 90) * 90;
                                    vec.y = Mathf.Round(vec.y / 90) * 90;
                                    vec.z = Mathf.Round(vec.z / 90) * 90;
                                    ModulePlan.Visualization.rotation = Quaternion.Euler(vec);
                                }
                            }

                        }

                        if (doInteract)
                        {
                            string depositID = depositSnap ? hitInfo.collider.GetComponent<Deposit>().Data.DepositInstanceID : null;
                            PlaceConstructionHere(hitInfo.point, depositID);
                        }
                        else
                        {
                            newPrompt = Prompts.PlanConstructionZoneHint;
                        }
                    }
                    else if (depositSnap && hitInfo.collider.CompareTag("terrain"))
                    {
                        ModulePlan.IsValid = false;
                        ModulePlan.Visualization.position = hitInfo.point;
                    }
                    else
                    {
                        ModulePlan.IsValid = false;
                    }
                }
                else
                {
                    ModulePlan.IsValid = false;
                }
            }
            else
            {
                ModulePlan.IsValid = false;
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
                Equip(Slot.Unequipped);
                ToggleModuleBlueprintMode(false);
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

    private void TogglePrintableBlueprintMode(bool showPrinter, ThreeDPrinter printer = null)
    {
        FPSController.FreezeLook = showPrinter;
        GuiBridge.Instance.TogglePrinter(showPrinter, printer);
    }

    private bool CastRay(out RaycastHit hitInfo, QueryTriggerInteraction triggerInteraction, params string[] layerNames)
    {
        return Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, InteractionRaycastDistance, LayerMask.GetMask(layerNames), triggerInteraction);
    }

    private PromptInfo OnFoodHover(Collider collider, bool doInteract, PromptInfo eatHint, Matter mealType, AudioClip clip)
    {
        if (doInteract)
        {
            collider.transform.root.GetComponent<Habitat>().Eat(mealType);
            PlayInteractionClip(collider.transform.position, clip, true, 0.5f);
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
        }, PlacePowerline, Prompts.PowerPlugPrompts);
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

    private void PlaceConstructionHere(Vector3 point, string depositID = null)
    {
        Transform zoneT = (Transform)GameObject.Instantiate(ModuleBridge.Instance.ConstructionZonePrefab, ModulePlan.Visualization.position, ModulePlan.Visualization.rotation);

        ConstructionZone zone = zoneT.GetComponent<ConstructionZone>();

        zone.Initialize(ModulePlan.Type, depositID);

        if ((zone.RequiredResourceMask.Length == 0) ||
            (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt)))
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
            case Equipment.Multimeter:
                AlternativeCamera.cullingMask = 1 << ElectricityLayerIndex;
                AlternativeCamera.enabled = true;
                break;
            case Equipment.GPS:
                AlternativeCamera.cullingMask = 1 << RadarLayerIndex;
                AlternativeCamera.enabled = true;
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
#warning carried object drag changed and replaced with constant
        carriedObject = rigid;
        carriedObject.useGravity = false;
        //carriedObject.isKinematic = true;
        oldMass = rigid.mass;
        carriedObject.mass = 0f;
        carriedObject.velocity = Vector3.zero;
        carriedObject.transform.SetParent(this.transform);
        rigid.drag = 10f;
        rigid.angularDrag = 200f;
        snappable.OnPickedUp();
    }

    internal void DropObject()
    {
        if (carriedObject != null)
        {
            carriedObject.drag = 0f;
            carriedObject.angularDrag = .0f;
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
            DrivingRoverInput.ToggleDriver(false);
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
            DrivingRoverInput.ToggleDriver(true);

            FPSController.transform.SetParent(DrivingRoverInput.transform.Find("Enter").transform);
            FPSController.transform.localPosition = Vector3.zero;
            FPSController.transform.localRotation = Quaternion.identity;
            FPSController.InitializeMouseLook();
            FPSController.FreezeMovement = true;
            FPSController.CharacterController.enabled = false;
        }

        SurvivalTimer.Instance.RefreshResources(IsInVehicle, DrivingRoverInput);
        GuiBridge.Instance.RefreshSurvivalPanel(IsInVehicle, SurvivalTimer.Instance.IsInHabitat);
    }

    private void PlaceTube(Collider toBulkhead)
    {
        Transform newCorridorParent = PlaceRuntimeLinkingObject(selectedBulkhead, toBulkhead, tubePrefab, createdTubes);
        
        IHabitatModule habMod1 = selectedBulkhead.transform.root.GetComponent<IHabitatModule>();
        IHabitatModule habMod2 = toBulkhead.transform.root.GetComponent<IHabitatModule>();

        Corridor corridor = newCorridorParent.GetComponent<Corridor>();
        corridor.AssignConnections(habMod1, habMod2, selectedBulkhead.transform.parent, toBulkhead.transform.parent);
    }

    private void PlaceGasPipe(Collider collider)
    {
        Transform newPipeTransform = PlaceRuntimeLinkingObject(selectedGasValve, collider, gasPipePrefab, createdPipes);

        if (selectedCompound == Matter.Unspecified)
            selectedCompound = GetCompoundFromValve(collider);

        ModuleGameplay g1 = selectedGasValve.transform.root.GetComponent<ModuleGameplay>(), g2 = collider.transform.root.GetComponent<ModuleGameplay>();
        if (g1 != null && g2 != null)
        {
            newPipeTransform.GetComponent<Pipeline>().AssignConnections(selectedCompound, g1, g2, selectedGasValve.transform, collider.transform);

            selectedCompound = Matter.Unspecified;
            selectedGasValve = null;

            CurrentMode = InputMode.Normal;
            GuiBridge.Instance.RefreshMode();
        }
    }

    private void PlacePowerline(Collider collider)
    {
        Transform power = PlaceRuntimeLinkingObject(selectedPowerSocket, collider, powerlinePrefab, createdPowerlines);
        
        IPowerable g1 = selectedPowerSocket.transform.root.GetComponent<IPowerable>(), g2 = collider.transform.root.GetComponent<IPowerable>();
        if (g1 != null && g2 != null && g1 != g2)
        {
            power.GetComponent<Powerline>().AssignConnections(g1, g2, selectedPowerSocket.transform, collider.transform);

            selectedPowerSocket = null;
            CurrentMode = InputMode.Normal;
            GuiBridge.Instance.RefreshMode();
            PlayInteractionClip(g2.transform.position, Sfx.PlugIn, volumeScale: .5f);
        }
    }

    private static Transform PlaceRuntimeLinkingObject(
        Collider firstObject, 
        Collider otherObject, 
        Transform linkingObjectPrefab, 
        List<Transform> addToList)
    {
        Vector3 midpoint = Vector3.Lerp(firstObject.transform.position, otherObject.transform.position, 0.5f);
        Transform newObj = GameObject.Instantiate<Transform>(linkingObjectPrefab);

        newObj.position = midpoint;
        newObj.LookAt(otherObject.transform);

        addToList.Add(newObj);

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
        this.enabled = false;
        StartCoroutine(AfterKillPlayer(reason));
    }

    private IEnumerator AfterKillPlayer(string reason)
    {
        var rigid = togglePlayerWhileKill(true);
        for (float i = 0; i < 5f;)
        {
            yield return null;
            i += Time.deltaTime;
            if (i > 2f)
            {
                rigid.angularDrag += .15f;
                rigid.drag += .15f;
            }
        }
        GuiBridge.Instance.ShowKillMenu(reason);
        togglePlayerAfterKill(true);
    }
    private Rigidbody togglePlayerWhileKill(bool killt)
    {
        SurvivalTimer.Instance.enabled = !killt;
        FPSController.enabled = !killt;
        this.enabled = !killt;
        FPSController.CharacterController.enabled = !killt;
        if (killt)
        {
            var rigid = Camera.main.gameObject.AddComponent(typeof(Rigidbody)) as Rigidbody;
            var collider = Camera.main.gameObject.AddComponent(typeof(CapsuleCollider)) as CapsuleCollider;
            collider.radius = .5f;
            collider.height = 1.5f;
            rigid.AddRelativeForce(Vector3.right * .5f, ForceMode.Impulse);
            Camera.main.transform.SetParent(null);
            return rigid;
        }
        else
        {
            Component.Destroy(Camera.main.gameObject.GetComponent<Rigidbody>());
            Component.Destroy(Camera.main.gameObject.GetComponent<CapsuleCollider>());
            Camera.main.transform.SetParent(FPSController.transform);
            Camera.main.transform.localPosition = new Vector3(0, 0.8f, 0f);
            Camera.main.transform.localRotation = Quaternion.identity;
            return null;
        }
    }
    private void togglePlayerAfterKill(bool killt)
    {
        Cursor.visible = !killt;
        Cursor.lockState = killt ? CursorLockMode.None : CursorLockMode.Confined;
        Time.timeScale = killt ? 0f : 1f;
    }

    public void ResetAfterKillPlayer()
    {
        SurvivalTimer.Instance.Oxygen.ResetToMaximum();
        SurvivalTimer.Instance.Water.ResetToMaximum();
        SurvivalTimer.Instance.Food.ResetToMaximum();
        SurvivalTimer.Instance.Power.ResetToMaximum();
        togglePlayerWhileKill(false);
        togglePlayerAfterKill(false);
        FPSController.transform.position = Graveyard.Instance.GetGlobalLastGraveStareAtLocation();
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
        PlayInteractionClip(enterTranform.position, Sfx.StartSleep, true, .6f);
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

    private ITerminal CurrentTerminal;
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

        CurrentTerminal.Toggle(inTerminal);

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
    private const float DustRemovalPerGameSecond = 1 / 60f;

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
        else if (doInteract)
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
