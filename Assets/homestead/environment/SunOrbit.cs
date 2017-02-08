using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;

public delegate void HandleHourChange(int sol, float hour);
public delegate void HandleSolChange(int sol);

public class SunOrbit : MonoBehaviour {
    public Material Skybox;
    public Light GlobalLight;

    public Transform DuskAndDawnOnlyParent, StarsParent;

    internal const float MartianHoursPerDay = 24.7f;
    internal const float MartianMinutesPerDay = (24 * 60) + 40;
    internal const float MartianSecondsPerDay = MartianMinutesPerDay * 60;
    internal const float GameMinutesPerGameDay = 20;
    internal const float GameSecondsPerGameDay = GameMinutesPerGameDay * 60;

    internal const float MartianSecondsPerGameSecond = MartianSecondsPerDay / GameSecondsPerGameDay;

    internal const float GameSecondsPerMartianMinute = GameSecondsPerGameDay / MartianSecondsPerDay * 60;
    private const int MaximumSpeedTiers = 6;
    private const float MaximumTimeScale = 1f * 2f * 2f * 2f * 2f * 2f;
    internal float CurrentHour = 9;
    internal float CurrentMinute = 0;
    internal int CurrentSol = 1;
    public float HoursSinceSol0
    {
        get
        {
            return CurrentSol * MartianHoursPerDay + CurrentHour;
        }
    }
    internal event HandleHourChange OnHourChange;
    internal event HandleSolChange OnSolChange;

    internal static SunOrbit Instance;
    void Awake()
    {
        Instance = this;
    }

	// Use this for initialization
	void Start () {
        GetClockTextMeshes();
        UpdateClockSpeedArrows();
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
    internal TextMesh[] Clocks;

    // Update is called once per frame
    void Update () {
        CurrentMinute += Time.deltaTime * GameSecondsPerMartianMinute;

        if (CurrentMinute > 60f)
        {
            CurrentHour++;
            CurrentMinute = 60f - CurrentMinute;

            if (OnHourChange != null)
                OnHourChange(CurrentSol, CurrentHour);
        }

        if (CurrentHour > 24 && CurrentMinute > 40f)
        {
            CurrentSol += 1;
            CurrentHour = 0;
            CurrentMinute = 40 - CurrentMinute;
            dawnMilestone = duskMilestone = dawnEnded = duskEnded = false;

            if (OnSolChange != null)
                OnSolChange(CurrentSol);
        }

        float percentOfDay = ((CurrentHour * 60) + CurrentMinute) / MartianMinutesPerDay;
        
        //todo: also set strength of shadows - strong at dawn/dust, much less strong around noon
        GlobalLight.transform.localRotation = Quaternion.Euler(-90 + (360 * percentOfDay), 0, 0);
        StarsParent.transform.localRotation = GlobalLight.transform.localRotation;

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

    //barry allen would be proud
    private int speedTier = 1;
    internal void SpeedUp()
    {
        Time.timeScale = Mathf.Min(MaximumTimeScale, Time.timeScale * 2f);
        speedTier = Math.Min(MaximumSpeedTiers, speedTier + 1);
        UpdateClockSpeedArrows();
    }

    private void UpdateClockSpeedArrows()
    {
        string arrows = new string('►', speedTier - 1);
        foreach(var t in this.Clocks)
        {
            t.transform.GetChild(0).GetComponent<TextMesh>().text = arrows; 
        }
    }

    internal void SlowDown()
    {
        Time.timeScale = Mathf.Max(1f, Time.timeScale / 2);
        speedTier = Math.Max(1, speedTier - 1);
        UpdateClockSpeedArrows();
    }
}
