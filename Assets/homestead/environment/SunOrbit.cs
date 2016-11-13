using UnityEngine;
using System.Collections;
using System;

public class SunOrbit : MonoBehaviour {
    public Material Skybox;
    public Light GlobalLight, Headlamp1, Headlamp2;

    internal float SecondsPerTick = 1f;
    internal float SecondsPerHour = 5f;

    internal float CurrentHour = 0;

	// Use this for initialization
	void Start () {
        //StartCoroutine(SunTick());
	}

    private IEnumerator SunTick()
    {
        while(this.isActiveAndEnabled)
        {
            yield return new WaitForSeconds(SecondsPerTick);
        }
    }

    // Update is called once per frame
    void Update () {
        CurrentHour += Time.deltaTime / SecondsPerHour;

        if (CurrentHour > 24f)
            CurrentHour = 24f - CurrentHour;

        GlobalLight.transform.localRotation = Quaternion.Euler(-90 + (360 * (CurrentHour / 24)), 0, 0);
        
        if (CurrentHour > 12f)
        {
            GlobalLight.intensity = Mathfx.Hermite(1, 0f, (CurrentHour - 12f) / 12f);
            Skybox.SetFloat("_Exposure", Mathfx.Hermite(8, 0f, (CurrentHour - 12f) / 12f));
        }
        else
        {
            GlobalLight.intensity = Mathfx.Hermite(0, 1f, CurrentHour / 12f);
            Skybox.SetFloat("_Exposure", Mathfx.Hermite(0f, 8f, (CurrentHour) / 12f));
        }

        if (CurrentHour > 6 && CurrentHour < 18 && Headlamp1.enabled)
        {
            Headlamp1.enabled = Headlamp2.enabled = false;
        }
        else if (CurrentHour > 18 && !Headlamp1.enabled)
        {
            Headlamp1.enabled = Headlamp2.enabled = true;
        }
    }
}
