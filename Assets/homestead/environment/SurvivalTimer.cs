using UnityEngine;
using System.Collections;
using System;

namespace RedHomestead
{
    public static class Constants
    {
        public const float KilogramsOxygenPerHour = 0.0972f;
        public const float CaloriesPerDay = 2400;
        public const float LitersOfWaterPerDay = 3f;
    }

    [Serializable]
    public struct SurvivalResourceAudio
    {
        public AudioClip WarningClip, CriticalClip;
    }

    public enum ConsumptionPeriod { Daily, Hourly }
}


[Serializable]
public abstract class SurvivalResource
{
    public readonly float ConsumptionPerSecond = .1f;
    public RedHomestead.SurvivalResourceAudio AudioClips;
    public float CurrentAmount = 100f;
    public float MaximumAmount = 100f;

    internal bool IsCritical = false;
    internal bool IsWarning = false;

    public int HoursLeftHint
    {
        get
        {
            return (int)Math.Ceiling(CurrentAmount / (ConsumptionPerSecond * 60));
        }
    }

    public SurvivalResource() { }
    public SurvivalResource(RedHomestead.ConsumptionPeriod consumes, float consumeAmountPerPeriod, float maxAmt, float? currAmt = null)
    {
        if (!currAmt.HasValue)
            currAmt = maxAmt;

        switch (consumes)
        {
            case RedHomestead.ConsumptionPeriod.Daily:
                this.ConsumptionPerSecond = consumeAmountPerPeriod / SunOrbit.MartianMinutesPerDay * SunOrbit.GameSecondsPerMartianMinute;
                break;
            case RedHomestead.ConsumptionPeriod.Hourly:
                this.ConsumptionPerSecond = consumeAmountPerPeriod / 60 * SunOrbit.GameSecondsPerMartianMinute;
                break;
        }

        this.MaximumAmount = maxAmt;
        this.CurrentAmount = currAmt.Value;
    }

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
    public SingleSurvivalResource(RedHomestead.ConsumptionPeriod consumes, float consumeAmountPerPeriod, float maxAmt, float? currAmt = null): base(consumes, consumeAmountPerPeriod, maxAmt, currAmt) { }

    protected override void DoConsume()
    {
        CurrentAmount -= Time.deltaTime * ConsumptionPerSecond;
        this.UpdateUI(CurrentAmount / MaximumAmount, HoursLeftHint);
    }

    public override void ResetToMaximum()
    {
        CurrentAmount = MaximumAmount;
        this.UpdateUI(1f, HoursLeftHint);
    }

    /// <summary>
    /// external call to UI code, float parameter is percentage of resource, 0-1f
    /// </summary>
    internal Action<float, int> UpdateUI;

    internal void Increment(float amount)
    {
        CurrentAmount += amount;

        if (CurrentAmount > MaximumAmount)
            CurrentAmount = MaximumAmount;
        else if (CurrentAmount < 0)
            CurrentAmount = 0f;

        this.UpdateUI(CurrentAmount / MaximumAmount, HoursLeftHint);
    }

    public override void Resupply(float additionalSecondsOfSupply)
    {
        CurrentAmount = Mathf.Min(MaximumAmount, CurrentAmount + (additionalSecondsOfSupply * this.ConsumptionPerSecond));
        this.UpdateUI(CurrentAmount / MaximumAmount, HoursLeftHint);
    }
}

[Serializable]
public class DoubleSurvivalResource : SurvivalResource
{
    public bool IsOnLastBar = false;

    public DoubleSurvivalResource() { }
    public DoubleSurvivalResource(RedHomestead.ConsumptionPeriod consumes, float consumeAmountPerPeriod, float maxAmt, float? currAmt = null): base(consumes, consumeAmountPerPeriod, maxAmt, currAmt) { }

    protected override void DoConsume()
    {
        CurrentAmount -= Time.deltaTime * ConsumptionPerSecond;
        
        if (IsOnLastBar)
            this.UpdateUI(0f, CurrentAmount / MaximumAmount, HoursLeftHint);

        else if (CurrentAmount <= 0f)
        {
            IsOnLastBar = true;
            CurrentAmount = MaximumAmount;
            this.UpdateUI(CurrentAmount / MaximumAmount, 0f, HoursLeftHint);
        }
        else
        {
            this.UpdateUI(CurrentAmount / MaximumAmount, 0f, HoursLeftHint);
        }
    }

    public override void ResetToMaximum()
    {
        CurrentAmount = MaximumAmount;
        IsOnLastBar = false;
        this.UpdateUI(1f, 0f, HoursLeftHint);
    }

    internal Action<float, float, int> UpdateUI;

    public override void Resupply(float additionalSecondsOfSupply)
    {
        CurrentAmount = Mathf.Min(MaximumAmount, CurrentAmount + (additionalSecondsOfSupply * this.ConsumptionPerSecond));
        this.UpdateUI(CurrentAmount / MaximumAmount, 0f, HoursLeftHint);
    }
}

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;

    public SingleSurvivalResource Oxygen =
        new SingleSurvivalResource(RedHomestead.ConsumptionPeriod.Hourly, RedHomestead.Constants.KilogramsOxygenPerHour, RedHomestead.Constants.KilogramsOxygenPerHour * 4f);

    public SingleSurvivalResource Water =
        new SingleSurvivalResource(RedHomestead.ConsumptionPeriod.Daily, RedHomestead.Constants.LitersOfWaterPerDay, RedHomestead.Constants.LitersOfWaterPerDay / 2);

    public SingleSurvivalResource Food = 
        new SingleSurvivalResource(RedHomestead.ConsumptionPeriod.Daily, RedHomestead.Constants.CaloriesPerDay, RedHomestead.Constants.CaloriesPerDay);

    public DoubleSurvivalResource Power = new DoubleSurvivalResource(RedHomestead.ConsumptionPeriod.Hourly, 1f, 6f);

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
    public Habitat CurrentHabitat
    {
        get; private set;
    }

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

            if (Oxygen.CurrentAmount < 0)
            {
                //todo: accept reason why you died: e.g. You asphyxiated
                KillPlayer();
                return;
            }

            Power.Consume();
            if (Power.IsOnLastBar && Power.CurrentAmount < 0f)
            {
                //todo: accept reason why you died: e.g. You froze
                KillPlayer();
                return;
            }
        }

        Water.Consume();

        if (Water.CurrentAmount < 0)
        {
            //todo: accept reason why you died: e.g. You terminally dehydrated
            KillPlayer();
            return;
        }

        Food.Consume();

        if (Food.CurrentAmount < 0)
        {
            //todo: accept reason why you died: e.g. You starved
            KillPlayer();
            return;
        }
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
