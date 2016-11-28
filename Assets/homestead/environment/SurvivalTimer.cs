using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class SurvivalResource
{
    public float CurrentAmount = 100f;
    public float MaximumAmount = 100f;
    public float ConsumptionPerSecond = .1f;

    public void Consume()
    {
        CurrentAmount -= Time.deltaTime * ConsumptionPerSecond;
        this.UpdateUI(CurrentAmount / MaximumAmount);
    }

    public void ResetToMaximum()
    {
        CurrentAmount = MaximumAmount;
        this.UpdateUI(1f);
    }

    internal Action<float> UpdateUI;
}

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;
    
    public SurvivalResource Oxygen = new SurvivalResource();
    public SurvivalResource Water = new SurvivalResource()
    {
        ConsumptionPerSecond = .05f
    };

    public bool UsingPackResources = true;

	void Awake () {
        Instance = this;
    }

    void Start()
    {
        Oxygen.UpdateUI = GuiBridge.Instance.RefreshOxygenBar;
        Water.UpdateUI = GuiBridge.Instance.RefreshWaterBar;
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
        }

        Water.Consume();

        if (Water.CurrentAmount < 0)
        {
            //todo: accept reason why you died: e.g. You terminally dehydrated
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

        UsingPackResources = false;
    }

    internal void FillWater()
    {
        Water.ResetToMaximum();
    }

    internal void UsePackResources()
    {
        UsingPackResources = true;
    }
}
