using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Airlock : MonoBehaviour {
    public const string OpenDoorName = "opendoor";
    public static Dictionary<Transform, Airlock> DoorToAirlock = new Dictionary<Transform, Airlock>();

    public Material LightOff, GreenLight, RedLight;
    public MeshRenderer PressurizedLight, DepressurizedLight;
    
    public Transform OuterDoor, InnerDoor;
    public bool IsPressurized, OuterDoorSealed = true, InnerDoorSealed = true;

    private Animator OuterAnimator, InnerAnimator;

	// Use this for initialization
	void Start () {
        DoorToAirlock[OuterDoor] = this;
        DoorToAirlock[InnerDoor] = this;

        OuterAnimator = OuterDoor.parent.GetComponent<Animator>();
        InnerAnimator = InnerDoor.parent.GetComponent<Animator>();

        RefreshState();
	}

    private void RefreshState()
    {
        OuterDoor.name = IsPressurized ? "lockeddoor" : OpenDoorName;
        InnerDoor.name = IsPressurized ? OpenDoorName : "lockeddoor";

        PressurizedLight.material = IsPressurized ? GreenLight : LightOff;
        DepressurizedLight.material = IsPressurized ? LightOff : RedLight;
    }

    public void Pressurize()
    {
        //only allow it if door is sealed properly
        if (OuterDoorSealed && !IsPressurized)
        {
            IsPressurized = true;
            RefreshState();
            SurvivalTimer.Instance.UseHabitatResources();
        }
    }

    public void Depressurize()
    {
        //only allow it if door is sealed properly
        if (InnerDoorSealed && IsPressurized)
        {
            IsPressurized = false;
            RefreshState();
            SurvivalTimer.Instance.UsePackResources();
        }
    }

    private void _ToggleDoor(Transform t)
    {
        if (t == InnerDoor)
        {
            if (IsPressurized)
            {
                InnerDoorSealed = !InnerDoorSealed;
                //if we're sealing it, go backwards
                InnerAnimator.SetFloat("speed", InnerDoorSealed ? -1f : 1f); 
                InnerAnimator.SetBool("open", !InnerDoorSealed);
            }
        }
        else if (t == OuterDoor)
        {
            if (!IsPressurized)
            {
                OuterDoorSealed = !OuterDoorSealed;
                //if we're sealing it, go backwards
                OuterAnimator.SetFloat("speed", OuterDoorSealed ? -1f : 1f);
                OuterAnimator.SetBool("open", !OuterDoorSealed);
            }
        }
    }

    public static void ToggleDoor(Transform t)
    {
        DoorToAirlock[t]._ToggleDoor(t);
    }
}

