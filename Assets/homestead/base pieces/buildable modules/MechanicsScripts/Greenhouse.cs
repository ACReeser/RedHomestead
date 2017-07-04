using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.Agriculture;
using RedHomestead.Electricity;

public class Greenhouse : FarmConverter, IHabitatModule
{
    public List<IHabitatModule> AdjacentModules { get; set; }

    private const float _harvestUnits = .5f;
    private const float _biomassPerTick = 5 / SunOrbit.GameSecondsPerGameDay;
    private const float OxygenPerDayPerLeafInKilograms = .0001715f;
    private const int MaximumLeafCount = 1000;
    private const float _oxygenAtFullBiomassPerTick = OxygenPerDayPerLeafInKilograms * MaximumLeafCount / SunOrbit.GameSecondsPerGameDay;
    private const float _waterPerTick = .2f / SunOrbit.GameSecondsPerGameDay;
    private const float _heaterWatts = 2 * ElectricityConstants.WattsPerBlock;

    public override float BiomassProductionPerTickInUnits
    {
        get
        {
            return _biomassPerTick;
        }
    }

    public override float HarvestThresholdInUnits
    {
        get
        {
            return 1f;
        }
    }

    public override bool IsOn { get; set; }

    public Habitat LinkedHabitat { get; set; }

    public override float OxygenProductionPerTickInUnits
    {
        get
        {
            return _oxygenAtFullBiomassPerTick * Get(RedHomestead.Simulation.Matter.Biomass).CurrentAmount;
        }
    }

    public override float WaterConsumptionPerTickInUnits
    {
        get
        {
            return _waterPerTick;
        }
    }

    public override float WattsConsumed
    {
        get
        {
            return IsOn ? _heaterWatts : 0f;
        }
    }

    public override void Convert()
    {
        this.FarmTick();
    }

    public override Module GetModuleType()
    {
        return Module.GreenhouseHall;
    }

    public override void OnEmergencyShutdown()
    {
#warning give IToggle a state override
        Toggle(HeatToggle);
    }

    public override void Report()
    {
    }

    protected override void OnHarvest(float harvestAmountUnits)
    {

    }
}
