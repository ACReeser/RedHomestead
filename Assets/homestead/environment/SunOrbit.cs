using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;
using RedHomestead.Persistence;

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

    internal bool RunTilMorning { get; private set; }

    internal event HandleHourChange OnHourChange;
    internal event HandleSolChange OnSolChange;

    internal static SunOrbit Instance;
    void Awake()
    {
        Instance = this;
    }

	// Use this for initialization
	void Start () {
        RefreshClockTextMeshes();
        UpdateClockSpeedArrows();
	}

    public void RefreshClockTextMeshes()
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
        Game.Current.Environment.CurrentMinute += Time.deltaTime * GameSecondsPerMartianMinute;

        if (Game.Current.Environment.CurrentMinute > 60f)
        {
            Game.Current.Environment.CurrentHour++;
            Game.Current.Environment.CurrentMinute = 60f - Game.Current.Environment.CurrentMinute;

            if (OnHourChange != null)
                OnHourChange(Game.Current.Environment.CurrentSol, Game.Current.Environment.CurrentHour);

            if (RunTilMorning && Game.Current.Environment.CurrentHour == 6)
            {
                ToggleSleepUntilMorning(false);
            }
        }

        if (Game.Current.Environment.CurrentHour > 24 && Game.Current.Environment.CurrentMinute > 40f)
        {
            Game.Current.Environment.CurrentSol += 1;
            Game.Current.Environment.CurrentHour = 0;
            Game.Current.Environment.CurrentMinute = 40 - Game.Current.Environment.CurrentMinute;
            dawnMilestone = duskMilestone = dawnEnded = duskEnded = false;

            if (OnSolChange != null)
                OnSolChange(Game.Current.Environment.CurrentSol);
        }

        float percentOfDay = ((Game.Current.Environment.CurrentHour * 60) + Game.Current.Environment.CurrentMinute) / MartianMinutesPerDay;
        
        //todo: also set strength of shadows - strong at dawn/dust, much less strong around noon
        GlobalLight.transform.localRotation = Quaternion.Euler(-90 + (360 * percentOfDay), 0, 0);
        StarsParent.transform.localRotation = GlobalLight.transform.localRotation;

        if (Game.Current.Environment.CurrentHour > 12f)
        {
            GlobalLight.intensity = Mathfx.Hermite(1, 0f, percentOfDay);
            Skybox.SetFloat("_Exposure", Mathfx.Hermite(8, 0f, percentOfDay));
        }
        else
        {
            GlobalLight.intensity = Mathfx.Hermite(0, 1f, percentOfDay);
            Skybox.SetFloat("_Exposure", Mathfx.Hermite(0f, 8f, percentOfDay));
        }

        if (Game.Current.Environment.CurrentHour > 6 && !dawnMilestone)
        {
            dawnMilestone = true;
            Dawn(true);
        }
        else if (Game.Current.Environment.CurrentHour > 7 && !dawnEnded)
        {
            dawnEnded = true;
            Dawn(false);
        }
        else if (Game.Current.Environment.CurrentHour > 18 && !duskMilestone)
        {
            duskMilestone = true;
            Dusk(true);
        }
        else if (Game.Current.Environment.CurrentHour > 18 && !duskEnded)
        {
            duskEnded = true;
            Dusk(false);
        }

        string textTime = String.Format("M{0}:{1}", ((int)Math.Truncate(Game.Current.Environment.CurrentHour)).ToString("D2"), ((int)Math.Truncate(Game.Current.Environment.CurrentMinute)).ToString("D2"));

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
    private int SpeedTier = 1;
    internal void SpeedUp()
    {
        Time.timeScale = Mathf.Min(MaximumTimeScale, Time.timeScale * 2f);
        SpeedTier = Math.Min(MaximumSpeedTiers, SpeedTier + 1);
        UpdateClockSpeedArrows();
    }

    private void UpdateClockSpeedArrows()
    {
        //dat unicode arrow
        string arrows = new string('►', SpeedTier - 1);
        foreach(var t in this.Clocks)
        {
            t.transform.GetChild(0).GetComponent<TextMesh>().text = arrows; 
        }
    }

    internal void SlowDown()
    {
        Time.timeScale = Mathf.Max(1f, Time.timeScale / 2);
        SpeedTier = Math.Max(1, SpeedTier - 1);
        UpdateClockSpeedArrows();
    }

    internal void ToggleSleepUntilMorning(bool startSleeping)
    {
        if (startSleeping)
        {
            Time.timeScale = 60f;
            SpeedTier = MaximumSpeedTiers;
            UpdateClockSpeedArrows();
            RunTilMorning = true;
        }
        else
        {
            Time.timeScale = 1f;
            RunTilMorning = false;
            PlayerInput.Instance.wakeyWakeySignal = true;
        }
    }

    internal void CheckEmergencyReset()
    {
        if (SpeedTier > 1f || RunTilMorning)
        {
            Time.timeScale = 1f;
            SpeedTier = 1;
            UpdateClockSpeedArrows();

            if (RunTilMorning)
            {
                RunTilMorning = false;
                PlayerInput.Instance.wakeyWakeySignal = true;
            }
        }
    }
}
