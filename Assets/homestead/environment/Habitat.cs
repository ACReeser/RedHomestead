using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Construction;

public class Habitat : MonoBehaviour //: MultipleResourceConverter
{

    private float _CurrentPowerRequirements = 1f;

    //public override float WattRequirementsPerTick
    //{
    //    get
    //    {
    //        return _CurrentPowerRequirements;
    //    }
    //}

    internal Dictionary<Compound, SumContainer> BasicResourceTotals = new Dictionary<Compound, SumContainer>();
    internal Dictionary<Resource, SumContainer> ComplexResourceTotals = new Dictionary<Resource, SumContainer>();

    private Sink WaterSink, OxygenSink;

    //public override void ClearHooks()
    //{
    //    throw new NotImplementedException();
    //}

    //public override void Convert()
    //{
    //    if (WaterSink != null)
    //    {
    //        //BasicResourceTotals[Compound.Water].FlowWithExternal(WaterSink);
    //    }
    //}

    //public override void OnSinkConnected(Sink s)
    //{
    //    if (s.HasContainerFor(Compound.Water))
    //        WaterSink = s;
    //}

    // Use this for initialization
    void Start () {
        //todo: move this to individual Stuff adds
        BasicResourceTotals[Compound.Water] = new SumContainer(20f)
        {
            SimpleCompoundType = Compound.Water,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
        BasicResourceTotals[Compound.Oxygen] = new SumContainer(20f)
        {
            SimpleCompoundType = Compound.Oxygen,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
        ComplexResourceTotals[Resource.Biomass] = new SumContainer(0f)
        {
            ComplexResourceType = Resource.Biomass,
            LastTickRateOfChange = 0,
            TotalCapacity = 0
        };
        ComplexResourceTotals[Resource.PreparedMeal] = new SumContainer(10f)
        {
            ComplexResourceType = Resource.PreparedMeal,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
        ComplexResourceTotals[Resource.MealPowder] = new SumContainer(20f)
        {
            ComplexResourceType = Resource.MealPowder,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
        ComplexResourceTotals[Resource.MealShake] = new SumContainer(6f)
        {
            ComplexResourceType = Resource.MealShake,
            LastTickRateOfChange = 0,
            TotalCapacity = 20f
        };
    }
	
	// Update is called once per frame
	void Update () {
	
	}

    public void HarvestPlanter(Transform t)
    {

    }

    public void ConsumePreparedMeal()
    {

    }

    public void ConsumeMealShake()
    {

    }

    public void ConsumeDrink()
    {

    }
}
