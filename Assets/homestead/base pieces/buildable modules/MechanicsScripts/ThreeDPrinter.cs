using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeDPrinter : MonoBehaviour, IDoorManager {
    private DoorRotationLerpContext lerp;

    public void ToggleDoor(Transform door)
    {
        //assumes all door transforms start shut
        if (lerp == null)
            lerp = new DoorRotationLerpContext(door, door.localRotation, Quaternion.Euler(-60, 0, -90f), .8f);
        
        lerp.Toggle(StartCoroutine);
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
