using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Buildings;

public enum HabitatType { LuxuryLander, Burrow } //Lander, Tent

[Serializable]
public class HabitatData : RedHomesteadData
{
    public string Name;
    public HabitatType Type;

    public override void AfterDeserialize(Transform t = null)
    {

    }

    protected override void BeforeMarshal(Transform t)
    {
        Name = t.name;
    }
}

public class Habitat : Converter
{
    private const float WaterPullPerTick = 1f;
    private const float OxygenPullPerTick = 1f;
    private float _CurrentPowerRequirements = 1f;

    //public override float WattRequirementsPerTick
    //{
    //    get
    //    {
    //        return _CurrentPowerRequirements;
    //    }
    //}
    
    public HabitatData HabitatData;

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
    
    void Awake () {
        if (Game.Current.IsNewGame)
        {
            print("Starting up new hab");
            HabitatData = new HabitatData();
            InitializeStartingData();
        }
    }
	
	// Update is called once per frame
	void Update () {
	
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
        if (Data.Containers[Matter.Biomass].CurrentAmount > 0 && Data.Containers[Matter.OrganicMeal].AvailableCapacity >= 1f)
        {
            Data.Containers[Matter.Biomass].Pull(1f);
            Data.Containers[Matter.OrganicMeal].Push(1f);
        }
    }

    public void PreparePowderToShake()
    {
        if (Data.Containers[Matter.MealPowder].CurrentAmount > 0 && Data.Containers[Matter.MealShake].AvailableCapacity >= 1f)
        {
            Data.Containers[Matter.MealPowder].Pull(1f);
            Data.Containers[Matter.MealShake].Push(1f);
        }
    }
    
    public override void Report()
    {
        
    }

    public override Module GetModuleType()
    {
        return Module.Habitat;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary()
        {
            { Matter.Water, new ResourceContainer(10f)
                {
                    MatterType = Matter.Water,
                    TotalCapacity = 20f
                }
            },
            { Matter.Oxygen, new ResourceContainer(20f)
            {
                MatterType = Matter.Oxygen,
                TotalCapacity = 20f
            }},
            { Matter.Biomass, new ResourceContainer(0f)
            {
                MatterType = Matter.Biomass,
                TotalCapacity = 0
            }},
            { Matter.OrganicMeal, new ResourceContainer(10f)
            {
                MatterType = Matter.OrganicMeal,
                TotalCapacity = 18f
            }},
            { Matter.RationMeal, new ResourceContainer(10f)
            {
                MatterType = Matter.RationMeal,
                TotalCapacity = 18f
            }},
            { Matter.MealPowder, new ResourceContainer(20f)
            {
                MatterType = Matter.MealPowder,
                TotalCapacity = 36f
            }},
            { Matter.MealShake, new ResourceContainer(6f)
            {
                MatterType = Matter.MealShake,
                TotalCapacity = 36f
            }},

        };
    }
}
