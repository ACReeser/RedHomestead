﻿using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using RedHomestead.EVA;
using System.Linq;
using RedHomestead.Rovers;

namespace RedHomestead
{

    [Serializable]
    public struct SurvivalResourceAudio
    {
        public AudioClip WarningClip, CriticalClip, DepletedClip;
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
    internal float PerkConsumptionCoefficient = 1f;
    internal float EffectiveConsumptionPerSecond
    {
        get { return Data.ConsumptionPerSecond * EnvironmentalConsumptionCoefficient * PerkConsumptionCoefficient; }
    }

    public int HoursLeftHint
    {
        get
        {
            return (int)Math.Ceiling(Data.Container.CurrentAmount / (EffectiveConsumptionPerSecond * 60));
        }
    }

    public SurvivalResource() { }

    public bool TryConsume()
    {
#if (DEVELOPMENT_BUILD || UNITY_EDITOR)
        if (PlayerInput.DoNotDisturb) return true;
#endif
        if (Game.Current.IsTutorial && SurvivalTimer.SkipConsume) return true;

        DoConsume();

        bool newCritical = HoursLeftHint <= 1;
        bool newWarning = !newCritical && HoursLeftHint <= 2f;

        if (newCritical && !this.IsCritical)
        {
            GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.AudioClips.CriticalClip);

            SunOrbit.Instance.CheckEmergencyReset();
        }
        else if (newWarning && !this.IsWarning)
        {
            GuiBridge.Instance.ComputerAudioSource.PlayOneShot(this.AudioClips.WarningClip);

            SunOrbit.Instance.CheckEmergencyReset();
        }

        this.IsCritical = newCritical;
        this.IsWarning = newWarning;
        return this.Data.Container.CurrentAmount > 0f;
    }

    protected abstract void DoConsume();

    public abstract void ResetToMaximum();
    public abstract void ResupplySeconds(float additionalSecondsOfSupply);
}

[Serializable]
public class SingleSurvivalResource : SurvivalResource
{
    public SingleSurvivalResource() { }

    protected override void DoConsume()
    {
        Data.Container.Pull(Time.deltaTime * EffectiveConsumptionPerSecond);
        this.UpdateUI(Data.Container.UtilizationPercentage, HoursLeftHint);
    }

    public override void ResetToMaximum()
    {
        Data.Container.Push(Data.Container.TotalCapacity);
        this.UpdateUI(1f, HoursLeftHint);
    }

    /// <summary>
    /// external call to UI code, float parameter is percentage of resource, 0-1f
    /// </summary>
    internal Action<float, int> UpdateUI;

    internal void Increment(float amount)
    {
        Data.Container.Push(amount);

        this.UpdateUI(Data.Container.UtilizationPercentage, HoursLeftHint);
    }

    public override void ResupplySeconds(float additionalSecondsOfSupply)
    {
        Data.Container.Push(additionalSecondsOfSupply * this.Data.ConsumptionPerSecond);

        this.UpdateUI(Data.Container.UtilizationPercentage, HoursLeftHint);
    }
}

public enum Temperature { Cold, Temperate, Hot }
public delegate void PlayerInHabitatHandler(bool isInHabitat);
public delegate void PlayerDeathHandler(GraveData newGrave);

public class DeprivationDurations
{
    public float OxygenDeprivationSurvivalTimeSeconds = 10f;
    public float PowerDeprivationSurvivalTimeSeconds = 20f;
    public float FoodDeprivationSurvivalTimeSeconds = 240f;
    public float WaterDeprivationSurvivalTimeSeconds = 120f;
}

public class SurvivalTimer : MonoBehaviour {
    public static SurvivalTimer Instance;

    public SingleSurvivalResource Oxygen = new SingleSurvivalResource();

    public SingleSurvivalResource Water = new SingleSurvivalResource();

    public SingleSurvivalResource Food = new SingleSurvivalResource();

    public SingleSurvivalResource Power = new SingleSurvivalResource();

    internal SingleSurvivalResource RoverOxygen, HabitatOxygen;
    internal SingleSurvivalResource RoverPower, HabitatPower;

    internal PackData Data { get; private set; }
    internal Temperature ExternalTemperature;
    public AudioClip IncomingSolarFlare;

    internal DeprivationDurations DeprivationDurations = new DeprivationDurations();

    public bool IsNotInHabitat
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
    public event PlayerInHabitatHandler OnPlayerInHabitatChange;
    public event PlayerDeathHandler OnPlayerDeath;

    public Habitat CurrentHabitat { get; private set; }
    public static bool SkipConsume { get; internal set; }

    void Awake () {
        Instance = this;
        CurrentHabitat = null;
    }

    public void Start()
    {
        SetData(Game.Current.Player.PackData);
        Oxygen.PerkConsumptionCoefficient = RedHomestead.Perks.PerkMultipliers.AirUsage;

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

        if (OnPlayerInHabitatChange != null)
            OnPlayerInHabitatChange(IsInHabitat);
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

    void Update ()
    {
        bool isNotInHab = IsNotInHabitat;

        bool nonSuitOxygenSuccess = false, nonSuitPowerSuccess = false;
        if (PlayerInput.Instance.IsInVehicle)
        {
            nonSuitOxygenSuccess = RoverOxygen.TryConsume();
            nonSuitPowerSuccess = RoverPower.TryConsume();
        }
        else if (IsInHabitat && CurrentHabitat.HasPower)
        {
            if (CurrentHabitat.IsOxygenOn)
            {
                nonSuitOxygenSuccess = HabitatOxygen.TryConsume();
            }

            if (CurrentHabitat.IsHeatOn)
            {
                nonSuitPowerSuccess = true; //HabitatPower.TryConsume(); //heater is just on, already factored in
            }
        }

        TryConsume(nonSuitOxygenSuccess, Oxygen);
        TryConsume(nonSuitPowerSuccess, Power);
        TryConsume(false, Water);
        TryConsume(false, Food);

        if (Data.IsInDeprivationMode())
        {
            if (Oxygen.Data.DeprivationSeconds > DeprivationDurations.OxygenDeprivationSurvivalTimeSeconds)
            {
                KillPlayer("ASPHYXIATION");
            }
            else if (Power.Data.DeprivationSeconds > DeprivationDurations.PowerDeprivationSurvivalTimeSeconds)
            {
                KillPlayer("EXPOSURE");
            }
            else if (Water.Data.DeprivationSeconds > DeprivationDurations.WaterDeprivationSurvivalTimeSeconds)
            {
                KillPlayer("DEHYDRATION");
            }
            else if (Food.Data.DeprivationSeconds > DeprivationDurations.FoodDeprivationSurvivalTimeSeconds)
            {
                KillPlayer("STARVATION");
            }
            else
            {
                PlayerInput.Instance.FPSController.SurvivalSpeedMultiplier = GetSpeedCoefficient();
                GuiBridge.Instance.RefreshDeprivationUX(this);
            }
        }
        else if (GuiBridge.Instance.ShowingDeprivationUX)
        {
            GuiBridge.Instance.RefreshDeprivationUX(this);
        }
    }

    /// <summary>
    /// returns a coefficient between 0 and 1
    /// </summary>
    /// <returns></returns>
    private float GetSpeedCoefficient()
    {
        float coeff = 1f;

        // -10% to -25%
        if (Power.Data.DeprivationSeconds > 0f)
        {
            coeff -= .1f + Mathf.Lerp(0f, .15f, Power.Data.DeprivationSeconds / DeprivationDurations.PowerDeprivationSurvivalTimeSeconds);
        }

        // -10% to -80%
        if (Food.Data.DeprivationSeconds > 0f)
        {
            coeff -= .1f + Mathf.Lerp(0f, .7f, Food.Data.DeprivationSeconds / DeprivationDurations.FoodDeprivationSurvivalTimeSeconds);
        }

        // 0% to -20%
        if (Water.Data.DeprivationSeconds > 0f)
        {
            coeff -= Mathf.Lerp(0f, .2f, Water.Data.DeprivationSeconds / DeprivationDurations.WaterDeprivationSurvivalTimeSeconds);
        }

        return Mathf.Max(0f, coeff);
    }

    private void TryConsume(bool nonSuitSuccess, SingleSurvivalResource resource)
    {
        if (!nonSuitSuccess && !resource.TryConsume())
        {
            if (resource.Data.DeprivationSeconds == 0f)
            {
                SunOrbit.Instance.CheckEmergencyReset();
                GuiBridge.Instance.ComputerAudioSource.PlayOneShot(resource.AudioClips.DepletedClip);
            }

            resource.Data.DeprivationSeconds += Time.deltaTime;
        }
        else
        {
            resource.Data.DeprivationSeconds = 0;
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
        GuiBridge.Instance.ResetDeprivationUX();
        var newGrave = new GraveData()
        {
            PlayerName = Game.Current.Player.Name,
            DeathReason = reason,
            StartSol = Game.Current.Player.SolStart,
            DeathSol = Game.Current.Environment.CurrentSol
        };

        var newGraves = new GraveData[1]
        {
            newGrave
        };
        if (Base.Current.Graves == null)
        {
            Base.Current.Graves = newGraves;
        }
        else
        {
            Base.Current.Graves = Base.Current.Graves.Concat(newGraves).ToArray();
        }

        if (this.OnPlayerDeath != null)
        {
            this.OnPlayerDeath(newGrave);
        }

        PlayerInput.Instance.KillPlayer(reason);
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

        this.OnEnterExitHabitat(true);
    }

    private void OnEnterExitHabitat(bool isInHabitat)
    {
        PlayerInput.Instance.Loadout.RefreshGadgetsBasedOnLocation();
        PlayerInput.Instance.SetPressure(IsInHabitat);
        this.RefreshResources(false);
        GuiBridge.Instance.RefreshSurvivalPanel(false, isInHabitat);
        if (OnPlayerInHabitatChange != null)
            OnPlayerInHabitatChange(IsInHabitat);
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
        this.OnEnterExitHabitat(false);
    }

    internal void ToggleRun(bool isRunning)
    {
        Oxygen.EnvironmentalConsumptionCoefficient = isRunning ? 1.5f : 1f;
        GuiBridge.Instance.RefreshSprintIcon(isRunning);
    }

    internal void RefreshResources(bool isInVehicle, RoverInput roverInput = null)
    {
        RoverOxygen = isInVehicle ? new SingleSurvivalResource()
        {
            Data = new PackResourceData(roverInput.Data.Oxygen, Oxygen.Data.ConsumptionPerSecond / Matter.Oxygen.Kilograms()),
            AudioClips = Oxygen.AudioClips,
            UpdateUI = GuiBridge.Instance.RefreshRoverOxygenBar
        } : null;
        RoverPower = isInVehicle ? new SingleSurvivalResource()
        {
            Data = new PackResourceData(roverInput.Data.EnergyContainer, Power.Data.ConsumptionPerSecond),
            AudioClips = Power.AudioClips,
            UpdateUI = GuiBridge.Instance.RefreshRoverPowerBar
        } : null;

        bool isInHabitat = CurrentHabitat != null;

        HabitatOxygen = isInHabitat ? new SingleSurvivalResource()
        {
            Data = new PackResourceData(CurrentHabitat.Data.Containers[Matter.Oxygen], Oxygen.Data.ConsumptionPerSecond / Matter.Oxygen.Kilograms()),
            AudioClips = Oxygen.AudioClips,
            UpdateUI = GuiBridge.Instance.RefreshHabitatOxygenBar
        } : null;
        HabitatPower = isInHabitat ? new SingleSurvivalResource()
        {
            Data = new PackResourceData(CurrentHabitat.HabitatData.EnergyContainer, Power.Data.ConsumptionPerSecond),
            AudioClips = Power.AudioClips,
            UpdateUI = GuiBridge.Instance.RefreshHabitatPowerBar
        } : null;
    }
}