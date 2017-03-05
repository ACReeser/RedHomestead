using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using RedHomestead.EVA;

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

    public int HoursLeftHint
    {
        get
        {
            return (int)Math.Ceiling(Data.Current / (Data.ConsumptionPerSecond * 60));
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
        Data.Current -= Time.deltaTime * Data.ConsumptionPerSecond;
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
        Data.Current -= Time.deltaTime * Data.ConsumptionPerSecond;
        
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

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;

    public SingleSurvivalResource Oxygen = new SingleSurvivalResource();

    public SingleSurvivalResource Water = new SingleSurvivalResource();

    public SingleSurvivalResource Food = new SingleSurvivalResource();

    public DoubleSurvivalResource Power = new DoubleSurvivalResource();

    internal PackData Data;

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
        Oxygen.UpdateUI = GuiBridge.Instance.RefreshOxygenBar;
        Water.UpdateUI = GuiBridge.Instance.RefreshWaterBar;
        Food.UpdateUI = GuiBridge.Instance.RefreshFoodBar;
        Power.UpdateUI = GuiBridge.Instance.RefreshPowerBar;
    }
	
	void Update () {
        if (UsingPackResources)
        {
            Oxygen.Consume();

            if (Oxygen.Data.Current < 0)
            {
                //todo: accept reason why you died: e.g. You asphyxiated
                KillPlayer();
                return;
            }

            Power.Consume();
            if (Power.IsOnLastBar && Power.Data.Current < 0f)
            {
                //todo: accept reason why you died: e.g. You froze
                KillPlayer();
                return;
            }
        }

        Water.Consume();

        if (Water.Data.Current < 0)
        {
            //todo: accept reason why you died: e.g. You terminally dehydrated
            KillPlayer();
            return;
        }

        Food.Consume();

        if (Food.Data.Current < 0)
        {
            //todo: accept reason why you died: e.g. You starved
            KillPlayer();
            return;
        }
    }

    internal void Assign(PackData data)
    {
        this.Data = data;
        Oxygen.Data = data.Oxygen;
        Power.Data = data.Power;
        Water.Data = data.Water;
        Food.Data = data.Food;
    }

    private void KillPlayer()
    {
        PlayerInput.Instance.KillPlayer();
        this.enabled = false;
    }

    internal void EnterHabitat(Habitat hab)
    {
        Oxygen.ResetToMaximum();
        Power.ResetToMaximum();

        CurrentHabitat = hab;
        PlayerInput.Instance.Loadout.RefreshGadgetsBasedOnLocation();
    }

    internal void FillWater()
    {
        Water.ResetToMaximum();
    }

    internal void EatFood(MealType meal)
    {
        Food.Increment(meal.GetCalories());
    }

    internal void BeginEVA()
    {
        CurrentHabitat = null;
        PlayerInput.Instance.Loadout.RefreshGadgetsBasedOnLocation();
    }
}

public enum MealType { Prepared = 0, Organic, Shake }

public static class MealTypeExtensions
{
    public static float GetCalories(this MealType meal)
    {
        switch (meal)
        {
            case MealType.Shake:
                return 600f;
            default:
                return 1200f;
        }
    }
}
