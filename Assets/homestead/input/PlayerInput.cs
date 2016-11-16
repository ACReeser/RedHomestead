using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Rovers;
using RedHomestead.Construction;

public class PlayerInput : MonoBehaviour {
    public enum InputMode { Default, Planning }
    public static PlayerInput Instance;
    public Transform tubePrefab;
    public CustomFPSController FPSController;
    public Transform ConstructionZonePrefab, SmallSolarFarmPrefab;
    public Material translucentPlanningMat;

    internal Module PlannedModule = Module.Unspecified;
    internal InputMode Mode = InputMode.Default;

    private Dictionary<Module, Transform> VisualizationCache = new Dictionary<Module, Transform>();
    private RoverInput DrivingRoverInput;
    private Collider selectedAirlock1, carriedObject;
    private List<Transform> createdTubes = new List<Transform>();
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
            }

            if (Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, 300f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
            {
                if (hitInfo.collider != null)
                {
                    if (hitInfo.collider.CompareTag("terrain"))
                    {
                        if (Mode == InputMode.Planning && PlannedModuleVisualization != null)
                        {
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
                    if (doInteract)
                    {
                        if (selectedAirlock1 == null)
                        {
                            selectedAirlock1 = hitInfo.collider;
                            newPrompt = GuiBridge.EndBulkheadBridgeHint;
                        }
                        else if (selectedAirlock1 != hitInfo.collider)
                        {
                            PlaceTube(hitInfo.collider);
                            newPrompt = GuiBridge.BulkheadBridgeCompletedPrompt;
                        }
                    }
                    else
                    {
                        if (selectedAirlock1 == null)
                        {
                            newPrompt = GuiBridge.StartBulkheadBridgeHint;
                        }
                        else if (selectedAirlock1 != hitInfo.collider)
                        {
                            newPrompt = GuiBridge.EndBulkheadBridgeHint;
                        }
                    }
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
            else if (doInteract && selectedAirlock1 == null)
            {
                selectedAirlock1 = null;
            }
        }
        else if (carriedObject != null)
        {
            DropObject();
        }

        if (newPrompt == null)
        {
            GuiBridge.Instance.HidePrompt();
        }
        else
        {
            GuiBridge.Instance.ShowPrompt(newPrompt);
        }
	}

    private void PlaceConstructionHere(Vector3 point)
    {
        Transform zoneT = (Transform)GameObject.Instantiate(ConstructionZonePrefab, PlannedModuleVisualization.position, PlannedModuleVisualization.rotation);

        ConstructionZone zone = zoneT.GetComponent<ConstructionZone>();
        zone.UnderConstruction = PlannedModule;
        zone.ModulePrefab = SmallSolarFarmPrefab;
        
        zone.InitializeRequirements();
        
        CycleMode();
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
        float distanceBetween = Vector3.Distance(selectedAirlock1.transform.position, collider.transform.position);

        Vector3 midpoint = Vector3.Lerp(selectedAirlock1.transform.position, collider.transform.position, 0.5f);
        Transform newTube = GameObject.Instantiate<Transform>(tubePrefab);

        newTube.position = midpoint;
        newTube.LookAt(selectedAirlock1.transform);
        newTube.localScale = new Vector3(newTube.localScale.x, newTube.localScale.y, (distanceBetween / 2f) + .2f);
        createdTubes.Add(newTube);
        collider.gameObject.SetActive(false);
        selectedAirlock1.gameObject.SetActive(false);
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
            PlannedModuleVisualization = GameObject.Instantiate<Transform>(this.SmallSolarFarmPrefab);
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
