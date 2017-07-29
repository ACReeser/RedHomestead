using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;
using UnityEngine;
using RedHomestead.Rovers;
using RedHomestead.Persistence;

public class Umbilical : Powerline
{
    private const float CapZOffset = 0.66f;
    private const int LinkNumberAdjustment = 0;
    private const int FABRIKAngleConstraint = 5;
    private const float FABRIKSolveTimeSeconds = 2f;
    
    public Transform UmbilicalLinkPrefab, UmbilicalCapPrefab;
    public float linkSeparation = .2f;

    internal Transform fromThing, toThing;

    private Transform 
        fromCap = null,
        toCap = null,
        rootLink = null, 
        lastLink = null;

    private FABRIK solver = null;
    private int numLinks;

    protected override void ShowVisuals(IPowerable g1, IPowerable g2, Transform transform1, Transform transform2)
    {
        Data.IsUmbilical = true;

        if (g1 is RoverInput)
            fromThing = transform1.GetChild(1);
        else
            fromThing = transform1;

        if (g2 is RoverInput)
            toThing = transform2.GetChild(1);
        else
            toThing = transform2;

        fromCap = CreateCap(fromThing);
        toCap = CreateCap(toThing);

        Vector3 fromPos = fromCap.position, 
            toPos = toCap.position;

        float distanceBetween = Vector3.Distance(fromPos, toPos);
        numLinks = Mathf.RoundToInt(distanceBetween / linkSeparation) + LinkNumberAdjustment;

        rootLink = GameObject.Instantiate<Transform>(UmbilicalLinkPrefab);
        rootLink.SetParent(this.transform);
        rootLink.position = fromPos;
        rootLink.rotation = fromThing.rotation;
        
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
    }

    private Transform CreateCap(Transform transform1)
    {
        var cap = GameObject.Instantiate<Transform>(UmbilicalCapPrefab);
        cap.position = transform1.position + transform1.TransformVector(Vector3.back * CapZOffset);
        cap.rotation = transform1.rotation;
        cap.localScale = transform1.lossyScale;
        cap.SetParent(this.transform);
        return cap;
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
