using RedHomestead.Radio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Radio
{
    public enum Voices { Neutral = 0, Soldier }

    public enum Callout { Confirmation, Assigned, Completed, Unassigned, Outside, Inside, Alarms, Electrical, Leak  }
}

public class TaskManager : MonoBehaviour {

    public VoicePack[] Voices;
    public AudioSource Radio;

	// Use this for initialization
	void Start () {
        StartCoroutine(PlayRandomClips(RedHomestead.Radio.Voices.Soldier, "Sarge"));
	}

    private IEnumerator PlayRandomClips(Voices v, string name)
    {
        while (isActiveAndEnabled)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(1f, 10f));
            Callout type = (Callout)UnityEngine.Random.Range((int)Callout.Confirmation, (int)Callout.Leak);
            PlayRadioCallout(v, type, name);
        }
    }

    internal void PlayRadioCallout(Voices v, Callout c, string name)
    {
        var clips = Voices[(int)v].Callouts[(int)c].Clips;
        StartCoroutine(PlayRadio(clips[UnityEngine.Random.Range(0, clips.Length)], name));
    }

    private IEnumerator PlayRadio(AudioClip c, string name)
    {
        GuiBridge.Instance.Radio.AgentName.text = name;
        GuiBridge.Instance.Radio.Group.alpha = 1;
        Radio.PlayOneShot(c);
        yield return new WaitForSecondsRealtime(c.length);
        float diedown = 1f;
        while(diedown > 0)
        {
            diedown -= Time.deltaTime;
            GuiBridge.Instance.Radio.Group.alpha = diedown;
            yield return null;
        }
        GuiBridge.Instance.Radio.Group.alpha = 0;
    }

    // Update is called once per frame
    void Update () {
		
	}
}
