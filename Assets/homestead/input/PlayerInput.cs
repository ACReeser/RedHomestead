using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class PlayerInput : MonoBehaviour {

    public Transform tubePrefab;
    public CustomFPSController FPSController;

    private UnityStandardAssets.Vehicles.Car.CarUserControl CarController;
    private Collider selectedAirlock1;
    private List<Transform> createdTubes = new List<Transform>();
    private bool playerIsOnFoot = true;
    private bool playerInVehicle
    {
        get
        {
            return !playerIsOnFoot;
        }
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

        bool doInteract = Input.GetKeyUp(KeyCode.E);
        PromptInfo newPrompt = null;

        RaycastHit hitInfo;
        if (Physics.Raycast(new Ray(this.transform.position, this.transform.forward), out hitInfo, 300f, LayerMask.GetMask("interaction"), QueryTriggerInteraction.Collide))
        {
            if (hitInfo.collider != null)
            {
                if (hitInfo.collider.gameObject.CompareTag("bulkhead"))
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
                        ToggleVehicle(hitInfo.collider.transform.GetComponent<UnityStandardAssets.Vehicles.Car.CarUserControl>());
                    }
                    else
                    {
                        newPrompt = GuiBridge.DriveRoverPrompt;
                    }
                }
            }
            else if (doInteract && selectedAirlock1 == null)
            {
                selectedAirlock1 = null;
            }
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

    private void ToggleVehicle(UnityStandardAssets.Vehicles.Car.CarUserControl carControl)
    {
        if (carControl == null && CarController != null)
        {
            playerIsOnFoot = true;
            CarController.enabled = false;
            FPSController.transform.position = CarController.transform.Find("Exit").transform.position;
            FPSController.transform.SetParent(null);
            FPSController.SuspendInput = false;
        }
        else
        {
            playerIsOnFoot = false;
            //FPSController.enabled = false;
            CarController = carControl;
            CarController.enabled = true;
            FPSController.transform.SetParent(CarController.transform.Find("Enter").transform);
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
        print(distanceBetween);
        createdTubes.Add(newTube);
        collider.gameObject.SetActive(false);
        selectedAirlock1.gameObject.SetActive(false);
    }
}
