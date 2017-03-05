using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Persistence;

[Serializable]
public class HabitatData : RedHomesteadData
{
    protected override void BeforeMarshal(MonoBehaviour container)
    {
    }
}

public class Habitat : Converter, IDataContainer<HabitatData>
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
    
    internal Dictionary<Matter, SumContainer> MatterTotals = new Dictionary<Matter, SumContainer>();

    private List<ISink> WaterSinks = new List<ISink>(), OxygenSinks = new List<ISink>();

    public override float WattRequirementsPerTick
    {
        get
        {
            return _CurrentPowerRequirements;
        }
    }

    public HabitatData Data { get; set; }

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
        if (externals.Count > 0 && MatterTotals[compound].AvailableCapacity >= pullPerTick)
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
            MatterTotals[compound].Push(pulled);
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
        //todo: move this to individual Stuff adds
        MatterTotals[Matter.Water] = new SumContainer(10f)
        {
            MatterType = Matter.Water,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
        MatterTotals[Matter.Oxygen] = new SumContainer(20f)
        {
            MatterType = Matter.Oxygen,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
        MatterTotals[Matter.Biomass] = new SumContainer(0f)
        {
            MatterType = Matter.Biomass,
            LastTickRateOfChange = 0,
            TotalCapacity = 0
        };
        MatterTotals[Matter.OrganicMeal] = new SumContainer(10f)
        {
            MatterType = Matter.OrganicMeal,
            LastTickRateOfChange = 0,
            TotalCapacity = 18f
        };
        MatterTotals[Matter.RationMeal] = new SumContainer(10f)
        {
            MatterType = Matter.RationMeal,
            LastTickRateOfChange = 0,
            TotalCapacity = 18f
        };
        MatterTotals[Matter.MealPowder] = new SumContainer(20f)
        {
            MatterType = Matter.MealPowder,
            LastTickRateOfChange = 0,
            TotalCapacity = 36f
        };
        MatterTotals[Matter.MealShake] = new SumContainer(6f)
        {
            MatterType = Matter.MealShake,
            LastTickRateOfChange = 0,
            TotalCapacity = 36f
        };
        Data = new HabitatData();
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void ImportResource(ResourceComponent r)
    {
        if (r.Data.ResourceType.IsStoredInHabitat())
        {
            float amountLeft = MatterTotals[r.Data.ResourceType].Push(r.Data.Quantity);
            
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
        if (MatterTotals[Matter.Biomass].CurrentAmount > 0 && MatterTotals[Matter.OrganicMeal].AvailableCapacity >= 1f)
        {
            MatterTotals[Matter.Biomass].Pull(1f);
            MatterTotals[Matter.OrganicMeal].Push(1f);
        }
    }

    public void PreparePowderToShake()
    {
        if (MatterTotals[Matter.MealPowder].CurrentAmount > 0 && MatterTotals[Matter.MealShake].AvailableCapacity >= 1f)
        {
            MatterTotals[Matter.MealPowder].Pull(1f);
            MatterTotals[Matter.MealShake].Push(1f);
        }
    }
    
    public override void Report()
    {
        
    }
}
