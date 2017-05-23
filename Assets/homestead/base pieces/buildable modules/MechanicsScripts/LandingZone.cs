using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Crafting;

public class LandingZone : MonoBehaviour, IDeliveryScript {
    public Transform landerPrefab;

    private Transform currentLander;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    public void Deliver(Order o)
    {
        Transform lander = GameObject.Instantiate<Transform>(landerPrefab);
        lander.position = this.transform.position + Vector3.up * 800f;
        lander.GetComponent<BounceLander>().Deliver(o);
    }

    public void Deliver(Dictionary<Matter, int> supplies, Dictionary<Craftable, int> craftables)
    {

    }
}
