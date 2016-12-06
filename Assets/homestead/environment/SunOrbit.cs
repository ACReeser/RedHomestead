using UnityEngine;
using System.Collections;
using System;

public class SunOrbit : MonoBehaviour {
    public Material Skybox;
    public Light GlobalLight;
    
    internal float GameSecondsPerMartianHour = 5f;
    internal static float MartianMinutesPerDay = (24 * 60) + 40;
    internal const float GameSecondsPerMartianMinute = 1 / 4f;//5 / ((24 * 60) + 40) / 60f;

    internal float CurrentHour = 0;
    internal float CurrentMinute = 0;

	// Use this for initialization
	void Start () {
	}

    private bool morningMilestone, eveningMilestone;

    // Update is called once per frame
    void Update () {
        CurrentMinute += Time.deltaTime / GameSecondsPerMartianMinute;

        if (CurrentMinute > 60f)
        {
            CurrentHour++;
            CurrentMinute = 60f - CurrentMinute;
        }

        if (CurrentHour > 24 && CurrentMinute > 40f)
        {
            CurrentHour = 0;
            CurrentMinute = 40 - CurrentMinute;
            morningMilestone = eveningMilestone = false;
        }

        float percentOfDay = ((CurrentHour * 60) + CurrentMinute) / MartianMinutesPerDay;

        GlobalLight.transform.localRotation = Quaternion.Euler(-90 + (360 * percentOfDay), 0, 0);
        
        if (CurrentHour > 12f)
        {
            GlobalLight.intensity = Mathfx.Hermite(1, 0f, percentOfDay);
            Skybox.SetFloat("_Exposure", Mathfx.Hermite(8, 0f, percentOfDay));
        }
        else
        {
            GlobalLight.intensity = Mathfx.Hermite(0, 1f, percentOfDay);
            Skybox.SetFloat("_Exposure", Mathfx.Hermite(0f, 8f, percentOfDay));
        }

        if (CurrentHour > 6 && !morningMilestone)
        {
            morningMilestone = true;
            if (PlayerInput.Instance.Headlamp1.enabled)
                PlayerInput.Instance.Headlamp1.enabled = PlayerInput.Instance.Headlamp2.enabled = false;
        }
        else if (CurrentHour > 18 && !eveningMilestone)
        {
            eveningMilestone = true;
            if (!PlayerInput.Instance.Headlamp1.enabled)
                PlayerInput.Instance.Headlamp1.enabled = PlayerInput.Instance.Headlamp2.enabled = true;
        }

        GuiBridge.Instance.TimeText.text = String.Format("M{0}:{1}", ((int)Math.Truncate(CurrentHour)).ToString("D2"), ((int)Math.Truncate(CurrentMinute)).ToString("D2"));
    }
}
