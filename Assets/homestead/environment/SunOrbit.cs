using UnityEngine;
using System.Collections;
using System;

public class SunOrbit : MonoBehaviour {
    public Material Skybox;
    public Light GlobalLight;

    public Transform DuskAndDawnOnlyParent;

    internal const float MartianMinutesPerDay = (24 * 60) + 40;
    internal const float MartianSecondsPerDay = MartianMinutesPerDay * 60;
    internal const float GameMinutesPerGameDay = 20;
    internal const float GameSecondsPerGameDay = GameMinutesPerGameDay * 60;

    internal const float MartianSecondsPerGameSecond = MartianSecondsPerDay / GameSecondsPerGameDay;

    internal const float GameSecondsPerMartianMinute = GameSecondsPerGameDay / MartianSecondsPerDay * 60;

    internal float CurrentHour = 9;
    internal float CurrentMinute = 0;

	// Use this for initialization
	void Start () {
        GetClockTextMeshes();
	}

    private void GetClockTextMeshes()
    {
        var clocks = GameObject.FindGameObjectsWithTag("clock");
        this.Clocks = new TextMesh[clocks.Length];
        for (int i = 0; i < clocks.Length; i++)
        {
            this.Clocks[i] = clocks[i].GetComponent<TextMesh>();
        }
    }

    private bool dawnMilestone, duskMilestone, dawnEnded, duskEnded;
    private TextMesh[] Clocks;

    // Update is called once per frame
    void Update () {
        CurrentMinute += Time.deltaTime * GameSecondsPerMartianMinute;

        if (CurrentMinute > 60f)
        {
            CurrentHour++;
            CurrentMinute = 60f - CurrentMinute;
        }

        if (CurrentHour > 24 && CurrentMinute > 40f)
        {
            CurrentHour = 0;
            CurrentMinute = 40 - CurrentMinute;
            dawnMilestone = duskMilestone = dawnEnded = duskEnded = false;
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

        if (CurrentHour > 6 && !dawnMilestone)
        {
            dawnMilestone = true;
            Dawn(true);
        }
        else if (CurrentHour > 7 && !dawnEnded)
        {
            dawnEnded = true;
            Dawn(false);
        }
        else if (CurrentHour > 18 && !duskMilestone)
        {
            duskMilestone = true;
            Dusk(true);
        }
        else if (CurrentHour > 18 && !duskEnded)
        {
            duskEnded = true;
            Dusk(false);
        }

        string textTime = String.Format("M{0}:{1}", ((int)Math.Truncate(CurrentHour)).ToString("D2"), ((int)Math.Truncate(CurrentMinute)).ToString("D2"));

        GuiBridge.Instance.TimeText.text = textTime;
        UpdateClocks(textTime);
    }

    private void UpdateClocks(string textTime)
    {
        if (this.Clocks != null)
        {
            for (int i = 0; i < this.Clocks.Length; i++)
            {
                this.Clocks[i].text = textTime;
            }
        }
    }

    private void Dawn(bool isStart)
    {
        if (isStart)
        {
            if (PlayerInput.Instance.Headlamp1.enabled)
                PlayerInput.Instance.Headlamp1.enabled = PlayerInput.Instance.Headlamp2.enabled = false;
        }

        ToggleDuskDawnParticleSystems(isStart);
    }

    private void Dusk(bool isStart)
    {
        if (isStart)
        {
            if (!PlayerInput.Instance.Headlamp1.enabled)
                PlayerInput.Instance.Headlamp1.enabled = PlayerInput.Instance.Headlamp2.enabled = true;
        }
        ToggleDuskDawnParticleSystems(isStart);
    }
    
    private void ToggleDuskDawnParticleSystems(bool isStart)
    {
        foreach (Transform t in DuskAndDawnOnlyParent)
        {
            var ps = t.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var emission = ps.emission;
                emission.enabled = isStart;
            }
        }
    }
}
