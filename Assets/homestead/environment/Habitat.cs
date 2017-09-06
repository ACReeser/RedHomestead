using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Buildings;
using RedHomestead.Electricity;
using RedHomestead.Industry;

public enum HabitatType { LuxuryLander, Burrow, Tent } //Lander, Tent

[Serializable]
public class HabitatExtraData : RedHomesteadData
{
    public string Name;
    public HabitatType Type;
    public EnergyContainer EnergyContainer;
    /// <summary>
    /// ONLY HABITAT.CS IS ALLOWED TO ACCCESS THIS
    /// </summary>
    public bool IsOxygenOn = true, IsHeatOn = true;

    [HideInInspector]
    public string ModuleInstanceID;

    public override void AfterDeserialize(Transform t = null)
    {

    }

    protected override void BeforeMarshal(Transform t)
    {
        this.ModuleInstanceID = t.GetComponent<Habitat>().Data.ModuleInstanceID;
        Name = t.name;
    }
}

public class Habitat : Converter, IVariablePowerConsumer, IBattery, IHabitatModule
{
    private const float WaterPullPerTick = 1f;
    private const float OxygenPullPerTick = 1f;
    private const float MaximumPowerRequirements = ElectricityConstants.WattsPerBlock * 4f;
    private const float OxygenLeakPerTickUnits = 1f / 500f;

    private float _CurrentPowerRequirements = MaximumPowerRequirements;

    internal ResourceChangeHandler OnResourceChange;
    public delegate void ResourceChangeHandler(params Matter[] type);
    internal PowerChangeHandler OnPowerChange;
    public delegate void PowerChangeHandler();

    public HabitatExtraData HabitatData;
    public PowerVisualization BatteryViz;

    public Transform[] bulkheads;
    public Transform[] Bulkheads { get { return bulkheads; } }

    public bool IsHeatOn { get { return this.HabitatData.IsHeatOn; } set { HabitatData.IsHeatOn = value; RecalcPowerRequirements(); } }
    public bool IsOxygenOn { get { return HabitatData.IsOxygenOn; } set { HabitatData.IsOxygenOn = value; RecalcPowerRequirements(); } }

    private void RecalcPowerRequirements()
    {
        this._CurrentPowerRequirements = ((HabitatData.IsHeatOn ? 2f : 0) + (HabitatData.IsOxygenOn ? 2f : 0)) * ElectricityConstants.WattsPerBlock;
        this.OnPowerChanged();

        (this as IPowerConsumer).RefreshVisualization();
    }

    new public bool IsOn { get { return this.HasPower; } set { } }

    public EnergyContainer EnergyContainer { get { return HabitatData.EnergyContainer; } }

    public GameObject EmergencyLight;
    public Light[] Lights;

    private List<ISink> WaterSinks = new List<ISink>(), OxygenSinks = new List<ISink>();

    public override float WattsConsumed
    {
        get
        {
            return _CurrentPowerRequirements;
        }
    }

    public float MaximumWattsConsumed
    {
        get
        {
            return _CurrentPowerRequirements;
        }
    }

    public Habitat LinkedHabitat
    {
        get
        {
            return this;
        }

        set
        {
            //noop;
        }
    }

    public List<IHabitatModule> AdjacentModules { get; set; }

    protected override void OnStart()
    {
        base.OnStart();
        FlowManager.Instance.PowerGrids.Add(this);
    }

    public override void ClearSinks()
    {
        WaterSinks.Clear();
        OxygenSinks.Clear();
    }

    internal void ImportProduceToOrganicMeal(ResourceComponent res)
    {
        if (res != null && res.Data.Container.MatterType == Matter.Produce)
        {
            float overage = Data.Containers[Matter.OrganicMeals].Push(res.Data.Container.Pull(res.Data.Container.CurrentAmount));

            if (OnResourceChange != null)
                OnResourceChange();

            if (overage <= 0)
            {
                Destroy(res.gameObject);
            }
        }
    }

    public override void Convert()
    {
        if (oxygenLeak)
        {
            Data.Containers[Matter.Oxygen].Pull(OxygenLeakPerTickUnits);

            if (this.OnResourceChange != null)
                this.OnResourceChange(Matter.Oxygen);
        }

        FlowWithExternal(Matter.Water, WaterSinks, WaterPullPerTick);
        FlowWithExternal(Matter.Oxygen, OxygenSinks, OxygenPullPerTick);
    }

    private void FlowWithExternal(Matter compound, List<ISink> externals, float pullPerTick)
    {
        if (externals.Count > 0 && Data.Containers[compound].AvailableCapacity >= pullPerTick)
        {
            float pulled = 0f;
            foreach (ISink s in externals)
            {
                pulled += s.Get(compound).Pull(pullPerTick);
                if (pulled >= pullPerTick)
                {
                    break;
                }
            }
            Data.Containers[compound].Push(pulled);
        }
    }

    public override void OnSinkConnected(ISink s)
    {
        if (s.HasContainerFor(Matter.Water))
            WaterSinks.Add(s);

        if (s.HasContainerFor(Matter.Oxygen))
            OxygenSinks.Add(s);
    }

    public void ImportResource(ResourceComponent r)
    {
        if (r.Data.Container.MatterType.IsStoredInHabitat())
        {
            float amountLeft = Data.Containers[r.Data.Container.MatterType].Push(r.Data.Container.CurrentAmount);
            
            if (amountLeft <= 0)
            {
                GameObject.Destroy(r.gameObject);
            }
        }
    }

    public void PrepareBiomassToPreparedMeal()
    {
        ConvertMealMatter(Matter.Biomass, Matter.OrganicMeals);
    }

    public void PreparePowderToShake()
    {
        ConvertMealMatter(Matter.MealPowders, Matter.MealShakes);
    }

    private void ConvertMealMatter(Matter from, Matter to)
    {
        if (Data.Containers[from].CurrentAmount >= from.CubicMetersPerMeal()  && Data.Containers[to].AvailableCapacity >= to.CubicMetersPerMeal())
        {
            Data.MatterHistory.Consume(from, Data.Containers[from].Pull(from.CubicMetersPerMeal()));
            Data.MatterHistory.Produce(to, Data.Containers[to].Push(to.CubicMetersPerMeal()));

            OnResourceChange(from, to);
        }
    }
    
    public override void Report()
    {
        
    }

    protected override string GetModuleInstanceID()
    {
        return this.name;
    }

    public override Module GetModuleType()
    {
        return Module.Habitat;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        print("Getting new habitat data");
        HabitatData = new HabitatExtraData()
        {
            EnergyContainer = new EnergyContainer()
            {
                TotalCapacity = ElectricityConstants.WattHoursPerBatteryBlock * 5f
            }
        };

        return new ResourceContainerDictionary()
        {
            { Matter.Water, new ResourceContainer(10f)
                {
                    MatterType = Matter.Water,
                    TotalCapacity = 20f
                }
            },
            { Matter.Oxygen, new ResourceContainer(10f)
            {
                MatterType = Matter.Oxygen,
                TotalCapacity = 10f
            }},
            { Matter.Biomass, new ResourceContainer(0f)
            {
                MatterType = Matter.Biomass,
                TotalCapacity = 1f
            }},
            { Matter.OrganicMeals, new ResourceContainer(0f)
            {
                MatterType = Matter.OrganicMeals,
                TotalCapacity = 1f
            }},
            { Matter.RationMeals, new ResourceContainer(Matter.RationMeals.CubicMetersPerMeal() * 6)
            {
                MatterType = Matter.RationMeals,
                TotalCapacity = 1f
            }},
            { Matter.MealPowders, new ResourceContainer(Matter.RationMeals.CubicMetersPerMeal() * 12)
            {
                MatterType = Matter.MealPowders,
                TotalCapacity = 1f
            }},
            { Matter.MealShakes, new ResourceContainer(0f)
            {
                MatterType = Matter.MealShakes,
                TotalCapacity = 1f
            }},

        };
    }

    internal void Eat(Matter mealType)
    {
        Data.MatterHistory.Consume(mealType,  Get(mealType).Pull(mealType.CubicMetersPerMeal()));
        SurvivalTimer.Instance.EatFood(mealType);
        OnResourceChange(mealType);
    }

    public void OnEmergencyShutdown()
    {
        if (EmergencyLight != null)
            EmergencyLight.SetActive(true);
    }

    private bool oxygenLeak;

    internal void ToggleOxygenLeak(bool leak)
    {
        oxygenLeak = leak;
    }

    public override void OnPowerChanged()
    {
        foreach(Light l in Lights)
        {
            l.enabled = this.HasPower;
            Behaviour halo = (Behaviour)l.GetComponent("Halo");
            if (halo != null)
                halo.enabled = this.HasPower;
        }

        if (!this.HasPower && EmergencyLight != null && !EmergencyLight.activeInHierarchy)
            EmergencyLight.SetActive(true);
        else if (this.HasPower && EmergencyLight != null && EmergencyLight.activeInHierarchy)
            EmergencyLight.SetActive(false);

        if (OnPowerChange != null)
            OnPowerChange();
    }
    
    public void PlayerToggleLights()
    {
        if (this.HasPower)
        {
            foreach (Light l in Lights)
            {
                l.enabled = !l.enabled;
            }
        }
    }
}
