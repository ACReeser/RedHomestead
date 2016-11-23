using UnityEngine;
using System.Collections;
using System;

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;

    public float OxygenAmount = 100f;
    public float OxygenMaximumAmount = 100f;
    public float OxygenConsumptionPerSecond = .1f;
    public bool UsingPackResources = true;

	void Awake () {
        Instance = this;
	}
	
	void Update () {
        if (UsingPackResources)
        {
            OxygenAmount -= Time.deltaTime * OxygenConsumptionPerSecond;
            GuiBridge.Instance.RefreshOxygenBar(OxygenAmount / OxygenMaximumAmount);

            if (OxygenAmount < 0)
            {
                PlayerInput.Instance.KillPlayer();
                this.enabled = false;
                return;
            }
        }
	}

    internal void UseHabitatResources()
    {
        OxygenAmount = OxygenMaximumAmount;
        GuiBridge.Instance.RefreshOxygenBar(OxygenAmount / OxygenMaximumAmount);

        UsingPackResources = false;
    }

    internal void UsePackResources()
    {
        UsingPackResources = true;
    }
}
