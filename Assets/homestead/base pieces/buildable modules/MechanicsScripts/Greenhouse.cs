using System;
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

public class Greenhouse : FarmConverter, IHabitatModule, ITriggerSubscriber, ICrateSnapper
{
    public List<IHabitatModule> AdjacentModules { get; set; }
    public MeshRenderer[] plants;
    public Color youngColor, oldColor;
    public Vector3 youngScale, oldScale;
    public Transform defaultSnap;

    private const float _harvestUnits = .25f;
    private const float _biomassPerTick = .25f / 5f / SunOrbit.GameSecondsPerGameDay;
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
            return _harvestUnits;
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

    protected override void OnHarvestComplete(float harvestAmountUnits)
    {
        if (outputs[0] != null)
        {
            outputs[0].Data.Container.Push(harvestAmountUnits);
        }
        else if (outputs[1] != null)
        {
            outputs[1].Data.Container.Push(harvestAmountUnits);
        }
        else
        {

            ResourceComponent res = BounceLander.CreateCratelike(Matter.Produce, harvestAmountUnits, defaultSnap.position, null, ContainerSize.Quarter).GetComponent<ResourceComponent>();
            res.SnapCrate(this, defaultSnap.position);
            outputs[0] = res;
        }
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

    private ResourceComponent[] outputs = new ResourceComponent[2];
    private Coroutine detachTimer = null;

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable moveSnap)
    {
        ResourceComponent res = c.GetComponent<ResourceComponent>();
        if (res != null && detachTimer == null && res.Data.Container.MatterType == Matter.Produce)
        {
            if (outputs[0] == null)
            {
                outputs[0] = res;
                res.SnapCrate(this, child.transform.position);
            }
            else if (outputs[1] == null)
            {
                outputs[1] = res;
                res.SnapCrate(this, child.transform.position);
            }
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        if (outputs[0] == (object)detaching)
        {
            outputs[0] = null;
            detachTimer = StartCoroutine(Timer());
        }
        else if (outputs[1] == (object)detaching)
        {
            outputs[1] = null;
            detachTimer = StartCoroutine(Timer());
        }
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(2f);
        detachTimer = null;
    }
}
