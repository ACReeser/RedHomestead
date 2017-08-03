using UnityEngine;
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

    public Habitat CurrentHabitat { get; private set; }

    void Awake () {
        Instance = this;
        CurrentHabitat = null;
    }

    void Start()
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

    void Update () {
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
        
        if (!nonSuitOxygenSuccess && !Oxygen.TryConsume())
        {
            KillPlayer("ASPHYXIATION");
            return;
        }
        
        if (!nonSuitPowerSuccess && !Power.TryConsume())
        {
            KillPlayer("EXPOSURE");
            return;
        }

        if (!Water.TryConsume())
        {
            KillPlayer("DEHYDRATION");
            return;
        }
        
        if (!Food.TryConsume())
        {
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