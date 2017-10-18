using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tutorial : MonoBehaviour, ITriggerSubscriber
{

    public RectTransform Mars101_WalkToLZ;

    public TriggerForwarder Mars101_LZTarget;

    public LandingZone LZ;

	// Use this for initialization
	void Start () {
        GuiBridge.Instance.Crosshair.gameObject.SetActive(false);
        Mars101_LZTarget.SetDad(this);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        if (child == Mars101_LZTarget && c.transform.CompareTag("Player"))
        {
            Mars101_WalkToLZ.gameObject.SetActive(false);
            Mars101_LZTarget.gameObject.SetActive(false);
            //LZ.Deliver()
        }
    }
}
