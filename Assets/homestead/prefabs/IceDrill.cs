using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using UnityEngine;
using RedHomestead.Persistence;

public class IceDrillData: FacingData
{

}

public class IceDrill : MonoBehaviour, IMovableSnappable {
    public ICrateSnapper SnappedTo { get; }
    public Transform Drill;
    public Transform OnOffHandle;
    public bool Drilling;

    private const float DrillDownLocalY = -.709f;
    public string GetText()
    {
        return "Ice Drill";
    }

    public void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition, Rigidbody jointRigid = null)
    {
    }

    public void UnsnapCrate()
    {
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
        StartCoroutine(MoveDrill());
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
