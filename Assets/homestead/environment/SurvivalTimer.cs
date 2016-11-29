using UnityEngine;
using System.Collections;
using System;

[Serializable]
public abstract class SurvivalResource
{
    public float CurrentAmount = 100f;
    public float MaximumAmount = 100f;
    public float ConsumptionPerSecond = .1f;

    public abstract void Consume();

    public abstract void ResetToMaximum();
}

public class SingleSurvivalResource : SurvivalResource
{
    public override void Consume()
    {
        CurrentAmount -= Time.deltaTime * ConsumptionPerSecond;
        this.UpdateUI(CurrentAmount / MaximumAmount);
    }

    public override void ResetToMaximum()
    {
        CurrentAmount = MaximumAmount;
        this.UpdateUI(1f);
    }
    internal Action<float> UpdateUI;
}

public class DoubleSurvivalResource : SurvivalResource
{
    public bool IsOnLastBar = false;

    public override void Consume()
    {
        CurrentAmount -= Time.deltaTime * ConsumptionPerSecond;
        
        if (IsOnLastBar)
            this.UpdateUI(0f, CurrentAmount / MaximumAmount);
        else if (CurrentAmount <= 0f)
        {
            IsOnLastBar = true;
            CurrentAmount = MaximumAmount;
            this.UpdateUI(CurrentAmount / MaximumAmount, 0f);
        }
        else
        {
            this.UpdateUI(CurrentAmount / MaximumAmount, 0f);
        }
    }

    public override void ResetToMaximum()
    {
        CurrentAmount = MaximumAmount;
        IsOnLastBar = false;
        this.UpdateUI(1f, 0f);
    }

    internal Action<float, float> UpdateUI;
}

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;
    
    public SingleSurvivalResource Oxygen = new SingleSurvivalResource();
    public SingleSurvivalResource Water = new SingleSurvivalResource()
    {
        ConsumptionPerSecond = .05f
    };
    public SingleSurvivalResource Food = new SingleSurvivalResource()
    {
        ConsumptionPerSecond = .025f
    };
    public DoubleSurvivalResource Power = new DoubleSurvivalResource();

    public bool UsingPackResources = true;

	void Awake () {
        Instance = this;
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

    internal void UseHabitatResources()
    {
        Oxygen.ResetToMaximum();
        Power.ResetToMaximum();

        UsingPackResources = false;
    }

    internal void FillWater()
    {
        Water.ResetToMaximum();
    }

    internal void EatFood()
    {
        Food.ResetToMaximum();
    }

    internal void UsePackResources()
    {
        UsingPackResources = true;
    }
}
