using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using UnityEngine;
using RedHomestead.Persistence;
using UnityEditor.Animations;

public class IceDrillData: FacingData
{

}

public class IceDrill : MovableSnappable {
    public Transform Drill;
    public Transform OnOffHandle;
    public bool Drilling, LegsDown;
    public Animator[] LegControllers;

    private const float DrillDownLocalY = -.709f;
    
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
            OnOffHandle.tag = "pumpHandle";
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
}
