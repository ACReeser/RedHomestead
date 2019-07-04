using RedHomestead.Radio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CalloutArray
{
    public AudioClip[] Clips;
}

public class VoicePack : MonoBehaviour {
    public CalloutArray[] Callouts;


    public AudioClip GetRandom()
    {
        Callout type = (Callout)UnityEngine.Random.Range((int)Callout.Confirmation, (int)Callout.Leak);
        return Callouts[(int)type].Clips[UnityEngine.Random.Range(0, Callouts[(int)type].Clips.Length)];
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
