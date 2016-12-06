using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class Airlock : MonoBehaviour {
    public const string OpenDoorName = "opendoor", ClosedDoorName = "closeddoor", LockedDoorName = "lockeddoor";
    public static Dictionary<Transform, Airlock> DoorToAirlock = new Dictionary<Transform, Airlock>();

    public Material LightOff, GreenLight, RedLight;
    public MeshRenderer PressurizedLight, DepressurizedLight;
    public Collider TerrainCollider;

    public Transform OuterDoor, InnerDoor;
    public bool IsPressurized, OuterDoorSealed = true, InnerDoorSealed = true;

    private Animator OuterAnimator, InnerAnimator;

	// Use this for initialization
	void Start () {
        DoorToAirlock[OuterDoor] = this;
        DoorToAirlock[InnerDoor] = this;

        OuterAnimator = OuterDoor.parent.GetComponent<Animator>();
        InnerAnimator = InnerDoor.parent.GetComponent<Animator>();

        RefreshDoorAndLightState();
	}

    private void RefreshDoorAndLightState()
    {
        OuterDoor.name = IsPressurized ? LockedDoorName : ClosedDoorName;
        InnerDoor.name = IsPressurized ? ClosedDoorName : LockedDoorName;

        PressurizedLight.material = IsPressurized ? GreenLight : LightOff;
        DepressurizedLight.material = IsPressurized ? LightOff : RedLight;
    }

    public void Pressurize()
    {
        //only allow it if door is sealed properly
        if (OuterDoorSealed && !IsPressurized)
        {
            IsPressurized = true;
            RefreshDoorAndLightState();
            SurvivalTimer.Instance.UseHabitatResources();
            OutsideVisuals.ToggleAllParticles(false);
            SetPlayerTerrainCollision(true);
            PlayerInput.Instance.AvailableMode = PlayerInput.PlanningMode.Interiors;
        }
    }

    private void SetPlayerTerrainCollision(bool doIgnore)
    {
        if (TerrainCollider != null)
        {
            //todo: cache collider
            Physics.IgnoreCollision(PlayerInput.Instance.FPSController.GetComponent<Collider>(), TerrainCollider, doIgnore);
        }
    }

    public void Depressurize()
    {
        //only allow it if door is sealed properly
        if (InnerDoorSealed && IsPressurized)
        {
            IsPressurized = false;
            RefreshDoorAndLightState();
            SurvivalTimer.Instance.UsePackResources();
            OutsideVisuals.ToggleAllParticles(true);
            SetPlayerTerrainCollision(false);
            PlayerInput.Instance.AvailableMode = PlayerInput.PlanningMode.Exterior;
        }
    }

    private void _ToggleDoor(Transform t)
    {
        if (t == InnerDoor)
        {
            if (IsPressurized)
            {
                InnerDoorSealed = !InnerDoorSealed;
                InnerDoor.name = InnerDoorSealed ? ClosedDoorName : OpenDoorName;
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
                OuterDoor.name = OuterDoorSealed ? ClosedDoorName : OpenDoorName;
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

