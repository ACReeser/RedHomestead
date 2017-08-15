using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Toolbox : MonoBehaviour, IDoorManager
{
    private DoorRotationLerpContext lerp;
    private bool open = false;
    
    public void ToggleDoor(Transform door)
    {
        //assumes all door transforms start shut
        if (lerp == null)
            lerp = new DoorRotationLerpContext(door, door.localRotation, Quaternion.Euler(0, -90, 90f), .4f);

        lerp.Toggle(StartCoroutine);
    }
}
