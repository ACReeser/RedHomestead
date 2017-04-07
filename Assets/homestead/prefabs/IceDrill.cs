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

public class IceDrill : MovableSnappable, ResourcelessGameplay {
    public Transform Drill;
    public Transform OnOffHandle;
    public bool Drilling, LegsDown;
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
		if (Drilling)
        {
            Drill.Rotate(Vector3.forward, 2f, Space.Self);
        }
	}

    public void ToggleLegs()
    {
        LegsDown = !LegsDown;
        print("legs down now " + LegsDown);
        foreach(Animator LegController in LegControllers)
        {
            LegController.SetBool("LegDown", LegsDown);
        }
    }

    public void ToggleDrilling()
    {
        Drilling = !Drilling;
        RefreshHandle();
        StartCoroutine(MoveDrill());
    }

    private void RefreshHandle()
    {
        OnOffHandle.localRotation = Quaternion.Euler(0, Drilling ? -180 : -90, 0);
    }

    private IEnumerator MoveDrill()
    {
        while (Drilling && Drill.localPosition.y > DrillDownLocalY)
        {
            yield return null;
            Drill.localPosition = Drill.localPosition + Vector3.down * .3f * Time.deltaTime;
        }
        while (!Drilling && Drill.localPosition.y < 0f)
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
        if (Drilling)
            ToggleDrilling();

        if (LegsDown)
            ToggleLegs();

        OnOffHandle.tag = "Untagged";
    }

    public void OnPowerChanged()
    {
        print("ice drill power");
        if (HasPower && LegsDown)
            OnOffHandle.tag = "pumpHandle";
        else
            OnOffHandle.tag = "Untagged";
    }

    public void OnEmergencyShutdown()
    {
        if (Drilling)
        {
            ToggleDrilling();
        }        
    }
}
