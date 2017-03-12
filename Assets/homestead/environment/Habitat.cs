using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Buildings;

public enum HabitatType { LuxuryLander, Burrow } //Lander, Tent

[Serializable]
public class HabitatExtraData : RedHomesteadData
{
    public string Name;
    public HabitatType Type;
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

public class Habitat : Converter
{
    private const float WaterPullPerTick = 1f;
    private const float OxygenPullPerTick = 1f;
    private float _CurrentPowerRequirements = 1f;

    internal ResourceChangeHandler OnResourceChange;
    public delegate void ResourceChangeHandler(params Matter[] type);
    
    public HabitatExtraData HabitatData;

    private List<ISink> WaterSinks = new List<ISink>(), OxygenSinks = new List<ISink>();

    public override float WattRequirementsPerTick
    {
        get
        {
            return _CurrentPowerRequirements;
        }
    }


    public override void ClearHooks()
    {
        WaterSinks.Clear();
        OxygenSinks.Clear();
    }

    public override void Convert()
    {
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
        if (r.Data.ResourceType.IsStoredInHabitat())
        {
            float amountLeft = Data.Containers[r.Data.ResourceType].Push(r.Data.Quantity);
            
            if (amountLeft <= 0)
            {
                GameObject.Destroy(r.gameObject);
            }
            else
            {
                r.Data.Quantity = amountLeft;
            }
        }
    }

    public void PrepareBiomassToPreparedMeal()
    {
        ConvertMealMatter(Matter.Biomass, Matter.OrganicMeal);
    }

    public void PreparePowderToShake()
    {
        ConvertMealMatter(Matter.MealPowder, Matter.MealShake);
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
        if (this.Data != null)
            return this.Data.ModuleInstanceID;
        else
            return base.GetModuleInstanceID();
    }

    public override Module GetModuleType()
    {
        return Module.Habitat;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        print("Getting new habitat data");
        HabitatData = new HabitatExtraData();

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
            { Matter.OrganicMeal, new ResourceContainer(0f)
            {
                MatterType = Matter.OrganicMeal,
                TotalCapacity = 1f
            }},
            { Matter.RationMeal, new ResourceContainer(Matter.RationMeal.CubicMetersPerMeal() * 6)
            {
                MatterType = Matter.RationMeal,
                TotalCapacity = 1f
            }},
            { Matter.MealPowder, new ResourceContainer(Matter.RationMeal.CubicMetersPerMeal() * 12)
            {
                MatterType = Matter.MealPowder,
                TotalCapacity = 1f
            }},
            { Matter.MealShake, new ResourceContainer(0f)
            {
                MatterType = Matter.MealShake,
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
}
