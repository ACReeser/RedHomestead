using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Buildings;

public interface IDoorManager
{
    void ToggleDoor(Transform door);
}

public class Airlock : GenericBaseModule, IDoorManager {
    public const string OpenDoorName = "opendoor", ClosedDoorName = "closeddoor", LockedDoorName = "lockeddoor";
    
    public Collider TerrainCollider;
    public Color OnColor, OffColor;
    public SpriteRenderer PressurizedSprite, DepressurizedSprite;

    public Transform OuterDoor, InnerDoor, PressurizeButton, DepressurizeButton;
    public bool IsPressurized, OuterDoorSealed = true, InnerDoorSealed = true;

    private Animator OuterAnimator, InnerAnimator;

    // Use this for initialization
    protected override void OnStart () {
        OuterAnimator = OuterDoor.parent.GetComponent<Animator>();
        InnerAnimator = InnerDoor.parent.GetComponent<Animator>();

        RefreshDoorAndLightState();
	}

    private void RefreshDoorAndLightState()
    {
        OuterDoor.name = IsPressurized ? LockedDoorName : ClosedDoorName;
        InnerDoor.name = IsPressurized ? ClosedDoorName : LockedDoorName;

        PressurizedSprite.color = IsPressurized ? OffColor : OnColor;
        DepressurizedSprite.color = IsPressurized ? OnColor : OffColor;
    }

    public void Pressurize()
    {
        //only allow it if door is sealed properly
        if (LinkedHabitat != null && OuterDoorSealed && !IsPressurized)
        {
            IsPressurized = true;
            RefreshDoorAndLightState();
            SurvivalTimer.Instance.EnterHabitat(LinkedHabitat);
            SetPlayerTerrainCollision(true);
            RefreshSealedButtons();
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
            SurvivalTimer.Instance.BeginEVA();
            SetPlayerTerrainCollision(false);
            RefreshSealedButtons();
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
                //InnerAnimator.SetFloat("speed", InnerDoorSealed ? -1f : 1f); 
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
                //OuterAnimator.SetFloat("speed", OuterDoorSealed ? -1f : 1f);
                OuterAnimator.SetBool("open", !OuterDoorSealed);
            }
        }

        RefreshSealedButtons();
    }

    private void RefreshSealedButtons()
    {
        SetButtonColorsAndTag(PressurizeButton, OuterDoorSealed && InnerDoorSealed && !IsPressurized);
        SetButtonColorsAndTag(DepressurizeButton, OuterDoorSealed && InnerDoorSealed && IsPressurized);
    }

    private void SetButtonColorsAndTag(Transform t, bool isButtonEnabled)
    {
        Color newColor = isButtonEnabled ? new Color(255f, 255f, 255f, 1f) : new Color(255f, 255f, 255f, .25f);
        t.GetChild(0).GetComponent<SpriteRenderer>().color = newColor;
        t.GetChild(1).GetComponent<TextMesh>().color = newColor;
        t.tag = isButtonEnabled ? "button" : "Untagged";
    }

    public void ToggleDoor(Transform t)
    {
        _ToggleDoor(t);
    }

    void OnTriggerEnter(Collider other)
    {
        ResourceComponent comp = other.GetComponent<ResourceComponent>();
        if (comp != null && this.LinkedHabitat != null)
        {
            this.LinkedHabitat.ImportResource(comp);
        }
    }

    public override Module GetModuleType()
    {
        return Module.Airlock;
    }
}

