using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using UnityEngine;
using RedHomestead.Persistence;

public class IceDrillData: FacingData
{

}

public class IceDrill : MovableSnappable {
    public Transform Drill;
    public Transform OnOffHandle;
    public bool Drilling;

    private const float DrillDownLocalY = -.709f;
    public override string GetText()
    {
        return "Ice Drill";
    }

    // Use this for initialization
    void Start () {
        Drill.localPosition = Vector3.zero;
	}

    // Update is called once per frame
    void Update () {
		
	}

    public void ToggleDrilling()
    {
        Drilling = !Drilling;
        RefreshHandle();
        StartCoroutine(MoveDrill());
    }

    private void RefreshHandle()
    {
        OnOffHandle.rotation = Quaternion.Euler(0, Drilling ? -180 : -90, 0);
    }

    private IEnumerator MoveDrill()
    {
        while (Drilling && Drill.localPosition.y > DrillDownLocalY)
        {
            yield return null;
            Drill.localPosition = Drill.localPosition + Vector3.down * .2f * Time.deltaTime;
        }
    }
}
