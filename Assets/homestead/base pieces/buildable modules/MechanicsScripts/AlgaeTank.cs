using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using System;
using RedHomestead.Electricity;
using RedHomestead.Industry;
using RedHomestead.Agriculture;

public class AlgaeTank : FarmConverter, IPowerToggleable, ITriggerSubscriber, ICrateSnapper
{
    public AudioClip HandleChangeClip;

    // 760 kg/m2 per unit, per fourth
    internal const float LeastBiomassHarvestKilograms = 760f / 4f;
    // least harvest kg per five days per seconds per day
    internal const float BiomassPerSecond = LeastBiomassHarvestKilograms / 5f / SunOrbit.MartianSecondsPerDay;
    internal const float MaximumBiomass = 760f;
    

    public MeshFilter PowerCabinet;
    public Mesh OnMesh, OffMesh;
    public MeshRenderer algaeRenderer;
    public AnimatedTexture animatedTexture;
    public Transform CratePrefab, CrateAnchor;
    private ResourceComponent capturedResource;

    private const float _harvestUnits = .1f;
    private const float _biomassPerTick = .1f / 3f / SunOrbit.GameSecondsPerGameDay;

    //same as greenhouse
    private const float OxygenPerDayPerLeafInKilograms = .0001715f;
    private const int MaximumLeafCount = 1000;
    private const float _oxygenAtFullBiomassPerTick = OxygenPerDayPerLeafInKilograms * MaximumLeafCount / SunOrbit.GameSecondsPerGameDay;

    private const float _waterPerTick = .1f / SunOrbit.GameSecondsPerGameDay;
    private const float _heaterWatts = 2 * ElectricityConstants.WattsPerBlock;

    public override float WattsConsumed
    {
        get
        {
            return _heaterWatts;
        }
    }

    public override float WaterConsumptionPerTickInUnits
    {
        get
        {
            return _waterPerTick;
        }
    }

    public override float BiomassProductionPerTickInUnits
    {
        get
        {
            return _biomassPerTick;
        }
    }

    public override float OxygenProductionPerTickInUnits
    {
        get
        {
            return _oxygenAtFullBiomassPerTick * Get(RedHomestead.Simulation.Matter.Biomass).CurrentAmount;
        }
    }

    public override float HarvestThresholdInUnits
    {
        get
        {
            return _harvestUnits;
        }
    }

    public override void Convert()
    {
        this.FarmTick();
    }

    protected override void OnStart()
    {
        base.OnStart();
        RefreshPowerSwitch();
        RefreshAlgaeBubbleSpeed();
    }
    //GuiBridge.Instance.ShowNews(NewsSource.AlgaeHarvestable);
    

    public override void ClearHooks()
    {
        WaterIn = null;

        FlexData.IsWaterOn = WaterIn != null;
    }

    public override void OnSinkConnected(ISink s)
    {
        base.OnSinkConnected(s);

        FlexData.IsWaterOn = WaterIn != null;
    }

    public override void Report()
    {
        //todo: report v3: flow is "0/1 kWh" etc, flow and amount update over time
        //using some sort of UpdateReport() call (which reuses built text boxes)
        //todo: report v4: each row gets a graph over time that shows effciency or flow
        //print(String.Format("HasPower: {3} - Hydrogen in: {0} - Water out: {1} - Methane out: {2}", MatterHistory[Matter.Hydrogen].Consumed, MatterHistory[Matter.Water].Produced, MatterHistory[Matter.Methane].Produced, HasPower));
        GuiBridge.Instance.WriteReport(
            "Algae Tank",
            "1 kWh + 2kg H2O => 1kg Biomass + 1kg O2",
            "100%",
            "100%",
            new ReportIOData() { Name = "Power", Flow = "1 kW/h", Amount = Data.EnergyHistory[Energy.Electrical].Consumed + " kWh", Connected = HasPower },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Water", Flow = "1 kg/d", Amount = Data.MatterHistory[Matter.Water].Consumed + " kg", Connected = WaterIn != null },
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Biomass", Flow = "1 kg/d", Amount = Data.MatterHistory[Matter.Biomass].Produced + " kg", Connected = true },
            }
            );
    }

    public void TogglePower()
    {
        //only allow power to turn on when power is connected
        bool newPowerState = !IsOn;
        if (newPowerState && HasPower)
        {
            IsOn = newPowerState;
        }
        else
        {
            IsOn = false;
        }

        RefreshPowerSwitch();
        RefreshPowerSwitch();
    }

    private void RefreshPowerSwitch()
    {
        PowerCabinet.mesh = IsOn ? OnMesh : OffMesh;
        PowerCabinet.transform.GetChild(0).name = IsOn ? "on" : "off";

        if (IsOn)
            SoundSource.Play();
        else
            SoundSource.Stop();
    }

    protected override void OnHarvestComplete(float harvestAmountUnits)
    {
        if (capturedResource == null)
        {
            capturedResource = BounceLander.CreateCratelike(Matter.Produce, 0f, CrateAnchor.position, size: ContainerSize.Quarter).GetComponent<ResourceComponent>();
            //capturedResource.RefreshLabel();
            capturedResource.SnapCrate(this, CrateAnchor.position);
        }

        if (capturedResource.Data.Container.AvailableCapacity >= harvestAmountUnits)
        {
            capturedResource.Data.Container.Push(harvestAmountUnits);
        }
    }

    private Coroutine detachTimer;
    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        if (detachTimer == null && res is ResourceComponent)
        {
            if ((res as ResourceComponent).Data.Container.MatterType == Matter.Produce)
            {
                capturedResource = res as ResourceComponent;
                capturedResource.SnapCrate(this, CrateAnchor.position);
            }
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        capturedResource = null;
        detachTimer = StartCoroutine(DetachTimer());
    }

    private IEnumerator DetachTimer()
    {
        yield return new WaitForSeconds(1f);
        detachTimer = null;
    }

    public override Module GetModuleType()
    {
        return Module.AlgaeTank;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary()
        {
            {
                Matter.Water,  new ResourceContainer() {
                    MatterType = Matter.Water,
                    TotalCapacity = 1f
                }
            },
            {
                Matter.Biomass,  new ResourceContainer() {
                    MatterType = Matter.Biomass,
                    TotalCapacity = 1f
                }
            },
        };
    }
    

    public override void OnEmergencyShutdown()
    {
        
    }

    public Color algaeColor = new Color(0, 221f / 255f, 84 / 255f);
    protected override void RefreshFarmVisualization()
    {
        algaeColor.a = (this.Get(Matter.Biomass).CurrentAmount / this.HarvestThresholdInUnits) * 1.5f;
        algaeRenderer.material.color = algaeColor;
    }

    public override void OnPowerChanged()
    {
        base.OnPowerChanged();
        RefreshAlgaeBubbleSpeed();
    }

    private void RefreshAlgaeBubbleSpeed()
    {
        animatedTexture.speed = HasPower && IsOn ? .2f : 0.01f;
    }
}

