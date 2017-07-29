using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;
using UnityEngine;

public class Umbilical : MonoBehaviour {
    public Transform UmbilicalLinkPrefab;
    public float linkSeparation = .2f;

    internal Transform oldFromParent, from, oldToParent, to;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    private Transform rootLink = null, lastLink = null;
    private FABRIK solver = null;
    private int numLinks;
    internal void AssignConnections(IPowerable g1, IPowerable g2, Transform transform1, Transform transform2)
    {
        from = transform1;
        oldFromParent = from.parent;

        to = transform2;
        oldToParent = to.parent;
        
        transform1.GetChild(0).gameObject.SetActive(true);
        transform2.GetChild(0).gameObject.SetActive(true);

        from.SetParent(this.transform);
        to.SetParent(this.transform);

        float distanceBetween = Vector3.Distance(from.position, to.position);
        numLinks = Mathf.RoundToInt(distanceBetween / linkSeparation) + 1;
        
        rootLink = GameObject.Instantiate<Transform>(UmbilicalLinkPrefab);
        rootLink.SetParent(this.transform);
        rootLink.position = from.position;
        rootLink.rotation = from.rotation;
        //rootLink.localScale = Vector3.one * 1.8f;

        //Transform end2 = GameObject.Instantiate<Transform>(UmbilicalLinkPrefab);
        //end2.SetParent(this.transform);
        //end2.position = to.position;
        //end2.rotation = to.rotation;

        //Vector3 direction = (from.position - to.position).normalized * linkSeparation;
        //Quaternion rotation = Quaternion.LookRotation(direction);
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
        solver.ConstrainAngle = 5;
        solver.initialTarget = to;

        StartCoroutine(StopSolver(solver));
    }

    private IEnumerator StopSolver(FABRIK solver)
    {
        yield return new WaitForSeconds(2f);
        solver.enabled = false;

        SetEnd(from, rootLink);
        SetEnd(to, lastLink);

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
    }

    private void SetEnd(Transform end, Transform link)
    {
        end.gameObject.GetComponent<Collider>().enabled = false;

        var endRigid = end.gameObject.AddComponent<Rigidbody>();
        endRigid.isKinematic = true;
        AddLimitedJoint(end);

        var linkRigid = link.GetComponent<Rigidbody>();
        AddLimitedJoint(end).connectedBody = linkRigid;

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
