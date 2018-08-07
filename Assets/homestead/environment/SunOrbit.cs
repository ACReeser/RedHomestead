using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;
using RedHomestead.Persistence;
using RedHomestead.Environment;

public delegate void HandleHourChange(int sol, float hour);
public delegate void HandleSolChange(int sol);
public delegate void DawnDuskHandler(bool isStart);

public class SunOrbit : MonoBehaviour {
    public Material Skybox, DuststormDay, DuststormNight;
    public Light GlobalLight;

    public Transform DuskAndDawnOnlyParent, StarsParent;

    /// <summary>
    /// 24.7h
    /// </summary>
    internal const float MartianHoursPerDay = 24.7f;
    /// <summary>
    /// 1,480min
    /// </summary>
    internal const float MartianMinutesPerDay = (24 * 60) + 40;
    /// <summary>
    /// 88,800s
    /// </summary>
    internal const float MartianSecondsPerDay = MartianMinutesPerDay * 60;
    /// <summary>
    /// 20.666666 game min
    /// </summary>
    internal const float GameMinutesPerGameDay = 24f + (3/10);
    /// <summary>
    /// 1,480 game s
    /// </summary>
    internal const float GameSecondsPerGameDay = 1480; // GameMinutesPerGameDay * 60;

    /// <summary>
    /// 60 mars s / 1 game s
    /// </summary>
    internal const float MartianSecondsPerGameSecond = MartianSecondsPerDay / GameSecondsPerGameDay;
    /// <summary>
    /// 1 game s / 1 mars min
    /// </summary>
    internal const float GameSecondsPerMartianMinute = 1f; //GameSecondsPerGameDay / MartianSecondsPerDay * 60;
    internal const float GameSecondsPerMartianHour   = 60f;

    private const int MaximumSpeedTiers = 6;
    private const float MaximumTimeScale = 1f * 2f * 2f * 2f * 2f * 2f;
    private const float NoonShadowIntensity = .4f;
    private const float MidnightSolarIntensity = -.4f;

    internal bool RunTilMorning { get; private set; }

    internal event HandleHourChange OnHourChange;
    internal event HandleSolChange OnSolChange;
    internal event DawnDuskHandler OnDawn;
    internal event DawnDuskHandler OnDusk;

    internal static SunOrbit Instance;
    internal static DustManager DustManager;

    void Awake()
    {
        Instance = this;
    }

	// Use this for initialization
	void Start () {
        RefreshClockTextMeshes();
        UpdateClockSpeedArrows();
        DustManager = new DustManager(this, Game.Current, Base.Current, SurvivalTimer.Instance, StartCoroutine, StopCoroutine);
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
                ToggleSleepUntilMorning(false, PlayerInput.WakeSignal.DayStart);
            }
        }

        if (Game.Current.Environment.CurrentHour > 24 && Game.Current.Environment.CurrentMinute > 40f)
        {
            NewDay();
        }

        float percentOfDay = ((Game.Current.Environment.CurrentHour * 60) + Game.Current.Environment.CurrentMinute) / MartianMinutesPerDay;
        
        GlobalLight.transform.localRotation = Quaternion.Euler(-90 + (360 * percentOfDay), 0, 0);
        StarsParent.transform.localRotation = GlobalLight.transform.localRotation;

        if (Game.Current.Environment.CurrentHour > 12f)
        {
            GlobalLight.intensity = Mathf.Max(0f, Mathfx.Hermite(1, MidnightSolarIntensity, (percentOfDay - .5f) * 2));
            GlobalLight.shadowStrength = Mathfx.Hermite(NoonShadowIntensity, 1f, (percentOfDay - .5f) * 2);
            Skybox.SetFloat("_Exposure", Mathf.Max(0f, Mathfx.Hermite(8, -.2f, percentOfDay)));
        }
        else
        {
            GlobalLight.intensity = Mathf.Max(0f, Mathfx.Hermite(MidnightSolarIntensity, 1f, percentOfDay * 2));
            GlobalLight.shadowStrength = Mathfx.Hermite(1f, NoonShadowIntensity, percentOfDay * 2);
            Skybox.SetFloat("_Exposure", Mathf.Max(0f, Mathfx.Hermite(-.2f, 8f, percentOfDay)));
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

    private void NewDay()
    {
        Game.Current.Environment.CurrentSol += 1;
        Game.Current.Environment.CurrentHour = 0;
        Game.Current.Environment.CurrentMinute = 40 - Game.Current.Environment.CurrentMinute;
        dawnMilestone = duskMilestone = dawnEnded = duskEnded = false;
        Game.Current.Score.AddScoringEvent(RedHomestead.Scoring.ScoreType.Survivor, GuiBridge.Instance);


        if (OnSolChange != null)
            OnSolChange(Game.Current.Environment.CurrentSol);
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
            if (PlayerInput.Instance.IsOnFoot && PlayerInput.Instance.IsInSuit && PlayerInput.Instance.Headlamp1.enabled)
                PlayerInput.Instance.Headlamp1.enabled = PlayerInput.Instance.Headlamp2.enabled = false;
        }

        if (OnDawn != null)
            OnDawn(isStart);
    }

    private void Dusk(bool isStart)
    {
        if (isStart)
        {
            if (PlayerInput.Instance.IsOnFoot && PlayerInput.Instance.IsInSuit && !PlayerInput.Instance.Headlamp1.enabled)
                PlayerInput.Instance.Headlamp1.enabled = PlayerInput.Instance.Headlamp2.enabled = true;
        }

        if (OnDusk != null)
            OnDusk(isStart);
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
        GuiBridge.Instance.TimeChevronText.text = arrows;
    }

    internal void SlowDown()
    {
        Time.timeScale = Mathf.Max(1f, Time.timeScale / 2);
        SpeedTier = Math.Max(1, SpeedTier - 1);
        UpdateClockSpeedArrows();
    }

    internal void ToggleSleepUntilMorning(bool startSleeping, PlayerInput.WakeSignal? signal = null)
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
            ResetToNormalTime();
            RunTilMorning = false;

            PlayerInput.Instance.wakeyWakeySignal = signal;
        }
    }

    internal void CheckEmergencyReset()
    {
        if (SpeedTier > 1f || RunTilMorning)
        {
            ResetToNormalTime();

            if (RunTilMorning)
            {
                RunTilMorning = false;
                PlayerInput.Instance.wakeyWakeySignal = PlayerInput.WakeSignal.ResourceRequired;
            }
        }
    }

    public void ResetToNormalTime()
    {
        Time.timeScale = 1f;
        SpeedTier = 1;
        UpdateClockSpeedArrows();
    }
}
