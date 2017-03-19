using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using UnityEngine;

public class IceDrill : MonoBehaviour, IMovableSnappable {
    public ICrateSnapper SnappedTo { get; }
    public Transform Drill;
    public bool Drilling;

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
	}

    // Update is called once per frame
    void Update () {
		
	}
}
