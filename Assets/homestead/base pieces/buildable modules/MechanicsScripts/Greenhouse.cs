﻿using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.Agriculture;
using RedHomestead.Electricity;
using RedHomestead.Simulation;

public class ToggleTerminalStateData
{
    public Color On = new Color(0f, 1f, 33f/255f, 1f);
    public Color Off = new Color(1f, 1f, 1f, 133f/255f);
    public Color Invalid = new Color(1f, 0f, 0f, 1f);

    public Quaternion ToggleOffPosition = Quaternion.Euler(-90f, 0f, 0f);

    public readonly static ToggleTerminalStateData Defaults = new ToggleTerminalStateData();
}

public class Greenhouse : FarmConverter, IHabitatModule
{
    public List<IHabitatModule> AdjacentModules { get; set; }
    public MeshRenderer[] plants;
    public Color youngColor, oldColor;
    public Vector3 youngScale, oldScale;

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
        RefreshIconsAndHandles();
    }

    public override void Report()
    {
    }

    protected override void OnHarvest(float harvestAmountUnits)
    {

    }

    protected override void RefreshFarmVisualization()
    {
        bool show = this.Get(Matter.Biomass).CurrentAmount > 0f;

        if (show)
        {
            float percentage = this.Get(Matter.Biomass).CurrentAmount / this.HarvestThresholdInUnits;
            Vector3 scale = Vector3.Lerp(youngScale, oldScale, percentage);
            Color color = Color.Lerp(youngColor, oldColor, percentage);

            foreach (var leaf in plants)
            {
                leaf.enabled = true;
                leaf.transform.localScale = scale;
                leaf.material.color = color;
            }
        }
        else
        {
            foreach (var leaf in plants)
            {
                leaf.enabled = false;
            }
        }
    }
}
