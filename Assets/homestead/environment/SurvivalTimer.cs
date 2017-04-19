using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using RedHomestead.EVA;
using System.Linq;

namespace RedHomestead
{

    [Serializable]
    public struct SurvivalResourceAudio
    {
        public AudioClip WarningClip, CriticalClip;
    }
}


[Serializable]
public abstract class SurvivalResource
{
    public RedHomestead.SurvivalResourceAudio AudioClips;
    internal PackResourceData Data;

    internal bool IsCritical = false;
    internal bool IsWarning = false;
    internal float EnvironmentalConsumptionCoefficient = 1f;

    public int HoursLeftHint
    {
        get
        {
            return (int)Math.Ceiling(Data.Current / (Data.ConsumptionPerSecond * EnvironmentalConsumptionCoefficient * 60));
        }
    }

    public SurvivalResource() { }

    public void Consume()
    {
        DoConsume();

        bool newCritical = HoursLeftHint <= 1;
        bool newWarning = !newCritical && HoursLeftHint <= 2f;

        if (newCritical && !this.IsCritical)
        {
            GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.AudioClips.CriticalClip);
        }
        else if (newWarning && !this.IsWarning)
        {
            GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.AudioClips.WarningClip);

            SunOrbit.Instance.CheckEmergencyReset();
        }

        this.IsCritical = newCritical;
        this.IsWarning = newWarning;
    }

    protected abstract void DoConsume();

    public abstract void ResetToMaximum();
    public abstract void Resupply(float additionalSecondsOfSupply);
}

[Serializable]
public class SingleSurvivalResource : SurvivalResource
{
    public SingleSurvivalResource() { }

    protected override void DoConsume()
    {
        Data.Current -= Time.deltaTime * Data.ConsumptionPerSecond * EnvironmentalConsumptionCoefficient;
        this.UpdateUI(Data.Current / Data.Maximum, HoursLeftHint);
    }

    public override void ResetToMaximum()
    {
        Data.Current = Data.Maximum;
        this.UpdateUI(1f, HoursLeftHint);
    }

    /// <summary>
    /// external call to UI code, float parameter is percentage of resource, 0-1f
    /// </summary>
    internal Action<float, int> UpdateUI;

    internal void Increment(float amount)
    {
        Data.Current += amount;

        if (Data.Current > Data.Maximum)
            Data.Current = Data.Maximum;
        else if (Data.Current < 0)
            Data.Current = 0f;

        this.UpdateUI(Data.Current / Data.Maximum, HoursLeftHint);
    }

    public override void Resupply(float additionalSecondsOfSupply)
    {
        Data.Current = Mathf.Min(Data.Maximum, Data.Current + (additionalSecondsOfSupply * this.Data.ConsumptionPerSecond));
        this.UpdateUI(Data.Current / Data.Maximum, HoursLeftHint);
    }
}

[Serializable]
public class DoubleSurvivalResource : SurvivalResource
{
    public bool IsOnLastBar = false;

    public DoubleSurvivalResource() { }

    protected override void DoConsume()
    {
        Data.Current -= Time.deltaTime * Data.ConsumptionPerSecond * EnvironmentalConsumptionCoefficient;
        
        if (IsOnLastBar)
            this.UpdateUI(0f, Data.Current / Data.Maximum, HoursLeftHint);

        else if (Data.Current <= 0f)
        {
            IsOnLastBar = true;
            Data.Current = Data.Maximum;
            this.UpdateUI(Data.Current / Data.Maximum, 0f, HoursLeftHint);
        }
        else
        {
            this.UpdateUI(Data.Current / Data.Maximum, 0f, HoursLeftHint);
        }
    }

    public override void ResetToMaximum()
    {
        Data.Current = Data.Maximum;
        IsOnLastBar = false;
        this.UpdateUI(1f, 0f, HoursLeftHint);
    }

    internal Action<float, float, int> UpdateUI;

    public override void Resupply(float additionalSecondsOfSupply)
    {
        Data.Current = Mathf.Min(Data.Maximum, Data.Current + (additionalSecondsOfSupply * this.Data.ConsumptionPerSecond));
        this.UpdateUI(Data.Current / Data.Maximum, 0f, HoursLeftHint);
    }
}

public enum Temperature { Cold, Temperate, Hot }

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;

    public SingleSurvivalResource Oxygen = new SingleSurvivalResource();

    public SingleSurvivalResource Water = new SingleSurvivalResource();

    public SingleSurvivalResource Food = new SingleSurvivalResource();

    public SingleSurvivalResource Power = new SingleSurvivalResource();

    internal PackData Data { get; private set; }
    internal Temperature ExternalTemperature;
    public AudioClip IncomingSolarFlare;

    public bool UsingPackResources
    {
        get
        {
            return CurrentHabitat == null;
        }
    }
    public bool IsInHabitat
    {
        get
        {
            return CurrentHabitat != null;
        }
    }

    public Habitat CurrentHabitat { get; private set; }

    void Awake () {
        Instance = this;
        CurrentHabitat = null;
    }

    void Start()
    {
        SetData(Game.Current.Player.PackData);

        if (!String.IsNullOrEmpty(this.Data.CurrentHabitatModuleInstanceID))
        {
            CurrentHabitat = GameObject.FindObjectsOfType<Habitat>().FirstOrDefault(x => x.Data.ModuleInstanceID == Data.CurrentHabitatModuleInstanceID);
            
            Airlock[] airlocks = CurrentHabitat.transform.GetComponentsInChildren<Airlock>();

            this.skipEnterHabitatPackRefill = true;
            foreach (Airlock a in airlocks)
            {
                a.Pressurize();
            }
        }

        Oxygen.UpdateUI = GuiBridge.Instance.RefreshOxygenBar;
        Water.UpdateUI = GuiBridge.Instance.RefreshWaterBar;
        Food.UpdateUI = GuiBridge.Instance.RefreshFoodBar;
        Power.UpdateUI = GuiBridge.Instance.RefreshPowerBar;
        SunOrbit.Instance.OnHourChange += OnHourChange;
        OnHourChange(Game.Current.Environment.CurrentSol, Game.Current.Environment.CurrentHour);
    }

    private void OnHourChange(int sol, float hour)
    {
        if (Game.Current.Environment.CurrentHour > 8 && Game.Current.Environment.CurrentHour < 16)
        {
            ExternalTemperature = Temperature.Hot;
            Power.EnvironmentalConsumptionCoefficient = .5f;
        }
        else if (Game.Current.Environment.CurrentHour < 4 | Game.Current.Environment.CurrentHour > 20)
        {
            ExternalTemperature = Temperature.Cold;
            Power.EnvironmentalConsumptionCoefficient = 2f;
        }
        else
        {
            ExternalTemperature = Temperature.Temperate;
            Power.EnvironmentalConsumptionCoefficient = 1f;
        }
        GuiBridge.Instance.RefreshTemperatureGauges();
    }

    void Update () {
        if (UsingPackResources || !CurrentHabitat.HasPower)
        {
            Oxygen.Consume();

            if (Oxygen.Data.Current < 0)
            {
                //todo: accept reason why you died: e.g. You asphyxiated
                KillPlayer("ASPHYXIATION");
                return;
            }

            Power.Consume();
            if (Power.Data.Current < 0f)
            {
                //todo: accept reason why you died: e.g. You froze
                KillPlayer("EXPOSURE");
                return;
            }
        }

        Water.Consume();

        if (Water.Data.Current < 0)
        {
            //todo: accept reason why you died: e.g. You terminally dehydrated
            KillPlayer("DEHYDRATION");
            return;
        }

        Food.Consume();

        if (Food.Data.Current < 0)
        {
            //todo: accept reason why you died: e.g. You starved
            KillPlayer("STARVATION");
            return;
        }
    }

    internal void SetData(PackData data)
    {
        this.Data = data;
        Oxygen.Data = data.Oxygen;
        Power.Data = data.Power;
        Water.Data = data.Water;
        Food.Data = data.Food;
    }

    private void KillPlayer(string reason)
    {
        PlayerInput.Instance.KillPlayer(reason);
        this.enabled = false;
    }

    private bool skipEnterHabitatPackRefill = false;
    internal void EnterHabitat(Habitat hab)
    {
        if (skipEnterHabitatPackRefill)
        {
            this.skipEnterHabitatPackRefill = false;
        }
        else if (hab.HasPower)
        {
            Oxygen.ResetToMaximum();
            Power.ResetToMaximum();
        }

        //hab should only be null if the Airlock making this call hasn't finished its Start method
        //which can happen on game load
        if (hab != null)
        {
            CurrentHabitat = hab;
            Data.CurrentHabitatModuleInstanceID = hab.Data.ModuleInstanceID;
        }

        PlayerInput.Instance.Loadout.RefreshGadgetsBasedOnLocation();
        PlayerInput.Instance.SetPressure(true);
    }

    internal void FillWater()
    {
        Water.ResetToMaximum();
    }

    internal void EatFood(Matter meal)
    {
        Food.Increment(meal.Calories());
    }

    internal void BeginEVA()
    {
        CurrentHabitat = null;
        PlayerInput.Instance.Loadout.RefreshGadgetsBasedOnLocation();
        PlayerInput.Instance.SetPressure(false);
    }

    internal void ToggleRun(bool isRunning)
    {
        Oxygen.EnvironmentalConsumptionCoefficient = isRunning ? 1.5f : 1f;
        GuiBridge.Instance.RefreshSprintIcon(isRunning);
    }
}