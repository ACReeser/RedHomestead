using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;
using UnityEngine;
using RedHomestead.Rovers;
using RedHomestead.Persistence;

public class Umbilical : Powerline
{
    private const float CapZOffset = 0.1f;
    private const int LinkNumberAdjustment = 0;
    private const int FABRIKAngleConstraint = 5;
    private const float FABRIKSolveTimeSeconds = 2f;
    
    public Transform UmbilicalLinkPrefab;
    public float linkSeparation = .2f;

    internal Transform fromThing, toThing;
    
    protected override Vector3 EndCapLocalPosition { get { return Vector3.back * CapZOffset; } }
    protected override Quaternion EndCapLocalRotation { get { return Quaternion.identity; } }
    protected override Vector3 EndCapWorldScale { get { return Vector3.one; } }

    private Transform 
        fromCap = null,
        toCap = null,
        rootLink = null, 
        lastLink = null;

    private FABRIK solver = null;
    private RoverStation station = null;
    private int numLinks;

    protected override void ShowVisuals(IPowerable g1, IPowerable g2)
    {
        if (g1 is RoverStation && g2 is RoverInput)
        {
            station = g1 as RoverStation;
            station.OnRoverAttachedChange(g2 as RoverInput);
        }
        else if (g2 is RoverStation && g1 is RoverInput)
        {
            station = g2 as RoverStation;
            station.OnRoverAttachedChange(g1 as RoverInput);
        }

        Data.IsUmbilical = true;

        fromCap = CreateCap(Data.fromPos, Data.fromRot, Data.fromScale);
        toCap = CreateCap(Data.toPos, Data.toRot, Data.toScale);

        Vector3 fromPos = fromCap.position, 
            toPos = toCap.position;

        float distanceBetween = Vector3.Distance(fromPos, toPos);
        numLinks = Mathf.RoundToInt(distanceBetween / linkSeparation) + LinkNumberAdjustment;

        rootLink = GameObject.Instantiate<Transform>(UmbilicalLinkPrefab);
        rootLink.SetParent(this.transform);
        rootLink.position = fromPos;
        rootLink.rotation = Data.fromRot;
        
        Transform parent = rootLink;
        for (int i = 0; i < numLinks - 1; i++)
        {
            Transform link = GameObject.Instantiate<Transform>(UmbilicalLinkPrefab);
            link.position = parent.position + Vector3.forward * linkSeparation;
            link.SetParent(parent);
            link.SetAsFirstSibling();
            parent = link;
        }

        lastLink = parent;

        solver = rootLink.gameObject.AddComponent<FABRIK>();
        solver.Constrain = true;
        solver.ConstrainAngle = FABRIKAngleConstraint;
        solver.initialTarget = toCap;

        StartCoroutine(StopSolver(solver));
    }

    protected override void HideVisuals()
    {
        station.OnRoverAttachedChange(null);
    }

    private IEnumerator StopSolver(FABRIK solver)
    {
        yield return new WaitForSeconds(FABRIKSolveTimeSeconds);
        solver.enabled = false;

        SetEnd(fromCap, rootLink);
        SetEnd(toCap, lastLink);

        Rigidbody parentRigid = rootLink.GetComponent<Rigidbody>();
        ConfigurableJoint parentJoint = parentRigid.GetComponent<ConfigurableJoint>();

        for (int i = 0; i < numLinks; i++)
        {
            parentRigid.isKinematic = false;
            parentRigid.transform.SetParent(rootLink.parent);

            Transform childT = parentRigid.transform.GetChild(0);
            if (childT.CompareTag("joint"))
            {
                Rigidbody child = childT.GetComponent<Rigidbody>();
                parentJoint.connectedBody = child;
                parentJoint = child.GetComponent<ConfigurableJoint>();
                parentRigid = child;
            }
        }

        //set the FABRIK root to under this for easier cleanup
        rootLink.parent.SetParent(this.transform);
    }

    private void SetEnd(Transform endCap, Transform link)
    {
        var endRigid = endCap.gameObject.GetComponent<Rigidbody>();
        
        AddLimitedJoint(endCap).connectedBody = link.GetComponent<Rigidbody>();

        if (link == lastLink)
            link.GetComponent<ConfigurableJoint>().connectedBody = endRigid;
    }

    private ConfigurableJoint AddLimitedJoint(Transform end)
    {
        var joint = end.gameObject.AddComponent<ConfigurableJoint>();
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;
        return joint;
    }
}
