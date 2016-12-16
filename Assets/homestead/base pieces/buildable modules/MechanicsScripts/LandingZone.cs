using UnityEngine;
using System.Collections;
using System;

public class LandingZone : MonoBehaviour {
    public Transform landerPrefab;

    private Transform currentLander;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    internal void CallLander()
    {
        if (currentLander == null)
        {
            currentLander = GameObject.Instantiate<Transform>(landerPrefab);
            currentLander.position = this.transform.position + Vector3.up * 800f;
        }
    }
}
