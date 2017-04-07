using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using UnityEngine;
using RedHomestead.Persistence;
using UnityEditor.Animations;
using RedHomestead.Electricity;

public class IceDrillData: FacingData
{

}

public class IceDrill : MovableSnappable, IPowerConsumer {
    public Transform Drill;
    public Transform OnOffHandle;
    public Transform Socket;
    public bool LegsDown;
    public Animator[] LegControllers;

    private const float DrillDownLocalY = -.709f;

    public float WattsConsumed { get { return ElectricityConstants.WattsPerBlock; } }

    public bool HasPower { get; set; }
    public bool IsOn { get; set; }

    public string PowerGridInstanceID { get; set; }

    public PowerVisualization _powerViz;
    public PowerVisualization PowerViz { get { return _powerViz; } }

    public override string GetText()
    {
        return "Ice Drill";
    }

    // Use this for initialization
    void Start () {
        Drill.localPosition = Vector3.zero;

        foreach (Animator LegController in LegControllers)
        {
            LegController.SetBool("LegDown", LegsDown);
        }

        this.InitializePowerVisualization();
    }

    // Update is called once per frame
    void Update () {
		if (IsOn)
        {
            Drill.Rotate(Vector3.forward, 2f, Space.Self);
        }
	}

    public void ToggleLegs()
    {
        LegsDown = !LegsDown;
        foreach(Animator LegController in LegControllers)
        {
            LegController.SetBool("LegDown", LegsDown);
        }
        Socket.tag = LegsDown ? "powerplug" : "Untagged";
    }

    public void ToggleDrilling(bool? state = null)
    {
        IsOn = state ?? !IsOn;
        RefreshHandle();
        StartCoroutine(MoveDrill());
        this.RefreshVisualization();
    }

    private void RefreshHandle()
    {
        OnOffHandle.localRotation = Quaternion.Euler(0, IsOn ? -180 : -90, 0);
    }

    private IEnumerator MoveDrill()
    {
        while (IsOn && Drill.localPosition.y > DrillDownLocalY)
        {
            yield return null;
            Drill.localPosition = Drill.localPosition + Vector3.down * .3f * Time.deltaTime;
        }
        while (!IsOn && Drill.localPosition.y < 0f)
        {
            yield return null;
            Drill.localPosition = Drill.localPosition - Vector3.down * .3f * Time.deltaTime;
        }
    }

    protected override void OnSnap()
    {
        if (this.SnappedTo is Deposit)
        {
            ToggleLegs();
        }
    }

    protected override void OnDetach()
    {
        if (IsOn)
            ToggleDrilling(false);

        if (LegsDown)
            ToggleLegs();

        OnOffHandle.tag = "Untagged";
    }

    public void OnPowerChanged()
    {
        if (HasPower && LegsDown)
            OnOffHandle.tag = "pumpHandle";
        else
            OnOffHandle.tag = "Untagged";
    }

    public void OnEmergencyShutdown()
    {
        ToggleDrilling(false);
    }
}
