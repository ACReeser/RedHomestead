using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Rovers;
using RedHomestead.Construction;

/// <summary>
/// Responsible for raycasting, modes, and gameplay input
/// </summary>
public class PlayerInput : MonoBehaviour {
    public enum InputMode { Default, Planning }
    public static PlayerInput Instance;

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
    public Transform ConstructionZonePrefab,
        //one of these for each module does NOT scale
        SmallSolarFarmPrefab,
        SmallGasTankPrefab,
        OxygenTank,
        SabatierPrefab,
        OreExtractorPrefab;
    /// <summary>
    /// the material to put on module prefabs
    /// when planning where to put them on the ground
    /// </summary>
    public Material translucentPlanningMat;

    internal Module PlannedModule = Module.Unspecified;
    internal InputMode Mode = InputMode.Default;

    /// <summary>
    /// Visualization == transparent preview of module to be built
    /// Cache == only create 1 of each type of module because creation is expensive
    /// </summary>
    private Dictionary<Module, Transform> VisualizationCache = new Dictionary<Module, Transform>();
    private RoverInput DrivingRoverInput;
    private Collider selectedAirlock1, selectedGasValve, selectedPowerSocket, carriedObject;
    private List<Transform> createdTubes = new List<Transform>();
    private List<Transform> createdPipes = new List<Transform>();
    private List<Transform> createdPowerlines = new List<Transform>();
    private bool playerIsOnFoot = true;

    private bool playerInVehicle
    {
        get
        {
            return !playerIsOnFoot;
        }
    }
    private Transform PlannedModuleVisualization;

    void Awake()
    {
        Instance = this;
    }

	// Update is called once per frame
	void Update () {

	    if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (playerIsOnFoot)
            {
                Application.Quit();
            }
            else if (playerInVehicle)
            {
                ToggleVehicle(null);
            }
        }

        if (Input.GetKeyUp(KeyCode.Tab))
        {
            CycleMode();
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            Headlamp1.enabled = Headlamp2.enabled = !Headlamp1.enabled;
        }

        PromptInfo newPrompt = null;
        bool doInteract = Input.GetKeyUp(KeyCode.E);
        RaycastHit hitInfo;
        if (Mode == InputMode.Planning)
        {
            if (PlannedModule == Module.Unspecified)
            {
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

            if (Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, 300f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                if (hitInfo.collider != null)
                {
                    if (hitInfo.collider.CompareTag("terrain"))
                    {
                        if (Mode == InputMode.Planning && PlannedModuleVisualization != null)
                        {
                            //TODO: raycast 3 more times (other 3 corners)
                            //then take the average height between them
                            //and invalidate the placement if it passes some threshold
                            PlannedModuleVisualization.position = hitInfo.point;

                            if (doInteract)
                            {
                                PlaceConstructionHere(hitInfo.point);
                            }
                            else
                            {
                                newPrompt = GuiBridge.PlanConstructionZoneHint;
                            }
                        }
                    }
                }
            }
        }
        else if (Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, 300f, LayerMask.GetMask("interaction"), QueryTriggerInteraction.Collide))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("movable"))
                {
                    if (carriedObject == null)
                    {
                        if (doInteract)
                        {
                            PickUpObject(hitInfo);
                        }
                        else
                        {
                            newPrompt = GuiBridge.PickupHint;
                        }
                    }
                    else
                    {
                        if (doInteract)
                        {
                            DropObject();
                        }
                        else
                        {
                            newPrompt = GuiBridge.DropHint;
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
                else if (hitInfo.collider.gameObject.CompareTag("valve"))
                {
                    newPrompt = OnGasValve(newPrompt, doInteract, hitInfo);
                }
                else if (playerIsOnFoot && hitInfo.collider.gameObject.CompareTag("rover"))
                {
                    if (doInteract)
                    {
                        ToggleVehicle(hitInfo.collider.transform.GetComponent<RoverInput>());
                    }
                    else
                    {
                        newPrompt = GuiBridge.DriveRoverPrompt;
                    }
                } 
                else if (hitInfo.collider.CompareTag("constructionzone"))
                {
                    ConstructionZone zone = hitInfo.collider.GetComponent<ConstructionZone>();

                    if (zone != null && carriedObject == null && zone.CanConstruct)
                    {
                        if (doInteract)
                        {
                            zone.WorkOnConstruction();
                        }
                        else
                        {
                            newPrompt = GuiBridge.ConstructHint;
                        }
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

    private PromptInfo OnPowerPlug(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedPowerSocket, value => selectedPowerSocket = value, PlacePowerPlug, GuiBridge.PowerPlugPrompts);
    }

    private PromptInfo OnBulkhead(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedAirlock1, value => selectedAirlock1 = value, PlaceTube, GuiBridge.BulkheadBridgePrompts);
    }

    private PromptInfo OnGasValve(PromptInfo newPrompt, bool doInteract, RaycastHit hitInfo)
    {
        return OnLinkable(doInteract, hitInfo, selectedGasValve, value => selectedGasValve = value, PlaceGasPipe, GuiBridge.GasPipePrompts);
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
                newPrompt = promptGroup.WhenCompleted;
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
        Transform zoneT = (Transform)GameObject.Instantiate(ConstructionZonePrefab, PlannedModuleVisualization.position, PlannedModuleVisualization.rotation);

        ConstructionZone zone = zoneT.GetComponent<ConstructionZone>();
        zone.UnderConstruction = PlannedModule;
        zone.ModulePrefab = GetPlannedModulePrefab();

        zone.InitializeRequirements();

        CycleMode();
    }

    private Transform GetPlannedModulePrefab()
    {
        switch(PlannedModule)
        {
            //storage
            case Module.SmallGasTank:
                return SmallGasTankPrefab;
            case Module.LargeGasTank:
                return OxygenTank;
            //extraction
            case Module.SabatierReactor:
                return SabatierPrefab;
            case Module.OreExtractor:
                return OreExtractorPrefab;
            //power
            case Module.SolarPanelSmall:
                return SmallSolarFarmPrefab;
            default:
                return SmallSolarFarmPrefab;
        }
    }

    private void CycleMode()
    {
        //todo: fix lazy code
        if (Mode == InputMode.Default)
            Mode = InputMode.Planning;
        else
            Mode = InputMode.Default;

        if (Mode == InputMode.Planning)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (this.PlannedModuleVisualization != null)
            {
                this.PlannedModuleVisualization.gameObject.SetActive(false);
                this.PlannedModuleVisualization = null;
            }
            this.PlannedModule = Module.Unspecified;
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
        if (roverInput == null && DrivingRoverInput != null)
        {
            playerIsOnFoot = true;
            DrivingRoverInput.enabled = false;
            FPSController.transform.position = DrivingRoverInput.transform.Find("Exit").transform.position;
            FPSController.transform.SetParent(null);
            FPSController.SuspendInput = false;
        }
        else
        {
            playerIsOnFoot = false;
            //FPSController.enabled = false;
            DrivingRoverInput = roverInput;
            DrivingRoverInput.enabled = true;
            FPSController.transform.SetParent(DrivingRoverInput.transform.Find("Enter").transform);
            FPSController.transform.localPosition = Vector3.zero;
            FPSController.transform.localRotation = Quaternion.identity;
            FPSController.SuspendInput = true;
        }
    }

    private void PlaceTube(Collider collider)
    {
        PlaceRuntimeLinkingObject(selectedAirlock1, collider, tubePrefab, createdTubes, true, .2f);
    }

    private void PlaceGasPipe(Collider collider)
    {
        PlaceRuntimeLinkingObject(selectedGasValve, collider, gasPipePrefab, createdPipes);
    }

    private void PlacePowerPlug(Collider collider)
    {
        PlaceRuntimeLinkingObject(selectedPowerSocket, collider, powerlinePrefab, createdPowerlines);

        collider.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
        selectedPowerSocket.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
    }

    private static void PlaceRuntimeLinkingObject(Collider firstObject, Collider otherObject, Transform linkingObjectPrefab, List<Transform> addToList, bool hideObjectEnds = false, float extraScale = 0f)
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
    }

    internal void PlanModule(Module planModule)
    {
        this.PlannedModule = planModule;

        if (VisualizationCache.ContainsKey(planModule))
        {
            PlannedModuleVisualization = VisualizationCache[planModule];
            PlannedModuleVisualization.gameObject.SetActive(true);
        }
        else
        {
            PlannedModuleVisualization = GameObject.Instantiate<Transform>(GetPlannedModulePrefab());
            VisualizationCache[planModule] = PlannedModuleVisualization;
            RecurseDisableColliderSetTranslucentRenderer(PlannedModuleVisualization);
        }        
    }

    private void RecurseDisableColliderSetTranslucentRenderer(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Collider c = child.GetComponent<Collider>();
            if (c != null)
                c.enabled = false;

            Renderer r = child.GetComponent<Renderer>();
            if (r != null)
                r.material = translucentPlanningMat;

            RecurseDisableColliderSetTranslucentRenderer(child);
        }
    }
}
