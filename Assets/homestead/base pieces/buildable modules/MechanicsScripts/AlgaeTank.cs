using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using System;
using RedHomestead.Electricity;
using RedHomestead.Industry;
using RedHomestead.Agriculture;

public class AlgaeTank : Converter, IPowerToggleable, IHarvestable, ICrateSnapper, ITriggerSubscriber, IPowerConsumer
{
    public AudioClip HandleChangeClip;

    // 760 kg/m2 per unit, per fourth
    internal const float LeastBiomassHarvestKilograms = 760f / 4f;
    // least harvest kg per five days per seconds per day
    internal const float BiomassPerSecond = LeastBiomassHarvestKilograms / 5f / SunOrbit.MartianSecondsPerDay;
    internal const float MaximumBiomass = 760f;

    internal float BiomassCollected = 0f;

#warning these are made up numbers
    internal float WaterPerSecond = .001f;
    internal float OxygenPerSecond = .001f;

    public MeshFilter PowerCabinet;
    public Mesh OnMesh, OffMesh;
    public Transform CratePrefab, CrateAnchor;
    private ResourceComponent capturedResource;

    public override float WattsConsumed
    {
        get
        {
            return 1f;
        }
    }

    private ISink WaterIn;
    private bool IsFullyConnected
    {
        get
        {
            return WaterIn != null;
        }
    }

    public bool IsOn { get; set; }

    public bool CanHarvest
    {
        get
        {
            return BiomassCollected > LeastBiomassHarvestKilograms &&
                ((capturedResource == null) || (capturedResource.Data.Container.CurrentAmount < MaximumBiomass));
        }
    }

    public float HarvestProgress
    {
        get
        {
            return 0f;
        }
    }

    public override void Convert()
    {
        if (HasPower && IsOn && IsFullyConnected && BiomassCollected < MaximumBiomass)
        {
            if (PullWater())
            {
                BuildUpBiomass();
            }
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        RefreshPowerSwitch();
    }

    private bool lastTickUnharvestable = true;
    private void BuildUpBiomass()
    {
        BiomassCollected += BiomassPerSecond;

        if (lastTickUnharvestable && CanHarvest)
        {
            GuiBridge.Instance.ShowNews(NewsSource.AlgaeHarvestable);
            lastTickUnharvestable = false;
        }
        else
        {
            lastTickUnharvestable = true;
        }
    }

    private float waterBuffer = 0f;
    private bool PullWater()
    {
        if (WaterIn != null)
        {
            float newWater = WaterIn.Get(Matter.Water).Pull(WaterPerSecond * Time.fixedDeltaTime);
            waterBuffer += newWater;
            Data.MatterHistory.Consume(Matter.Water, newWater);

            float waterThisTick = WaterPerSecond * Time.fixedDeltaTime;

            if (waterBuffer >= waterThisTick)
            {
                waterBuffer -= waterThisTick;
                return true;
            }
        }

        return false;
    }

    public override void ClearHooks()
    {
        WaterIn = null;
    }

    public override void OnSinkConnected(ISink s)
    {
        if (s.HasContainerFor(Matter.Water))
        {
            WaterIn = s;
        }
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

    public void Harvest()
    {
        if (capturedResource == null)
        {
            capturedResource = GameObject.Instantiate<Transform>(CratePrefab).GetComponent<ResourceComponent>();
            capturedResource.Data.Container = new ResourceContainer(Matter.Biomass, 0f);
            capturedResource.RefreshLabel();
            capturedResource.SnapCrate(this, CrateAnchor.position);
        }

        if (capturedResource.Data.Container.CurrentAmount < MaximumBiomass)
        {
            capturedResource.Data.Container.Push(BiomassCollected);
            BiomassCollected = 0;
        }
    }

    private Coroutine detachTimer;
    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        if (detachTimer == null && res is ResourceComponent)
        {
            if ((res as ResourceComponent).Data.Container.MatterType == Matter.Biomass)
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

    public void OnEmergencyShutdown()
    {
    }

    public void Harvest(float addtlProgress)
    {
    }

    public void CompleteHarvest()
    {
    }
}

