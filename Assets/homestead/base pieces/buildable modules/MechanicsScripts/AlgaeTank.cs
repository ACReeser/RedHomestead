using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using System;

public class AlgaeTank : Converter, IPowerToggleable, IHarvestable, ICrateSnapper, ITriggerSubscriber
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

    internal bool _isOn = false;

    public MeshFilter PowerCabinet;
    public Mesh OnMesh, OffMesh;
    public Transform CratePrefab, CrateAnchor;
    private ResourceComponent capturedResource;

    public override float WattRequirementsPerTick
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

    public bool IsOn
    {
        get
        {
            return _isOn;
        }
    }

    public bool CanHarvest
    {
        get
        {
            return BiomassCollected > LeastBiomassHarvestKilograms &&
                ((capturedResource == null) || (capturedResource.Info.Quantity < MaximumBiomass));
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
            MatterHistory.Consume(Matter.Water, newWater);

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
            new ReportIOData() { Name = "Power", Flow = "1 kW/h", Amount = EnergyHistory[Energy.Electrical].Consumed + " kWh", Connected = HasPower },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Water", Flow = "1 kg/d", Amount = MatterHistory[Matter.Water].Consumed + " kg", Connected = WaterIn != null },
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Biomass", Flow = "1 kg/d", Amount = MatterHistory[Matter.Biomass].Produced + " kg", Connected = true },
            }
            );
    }

    public void TogglePower()
    {
        //only allow power to turn on when power is connected
        bool newPowerState = !_isOn;
        if (newPowerState && HasPower)
        {
            _isOn = newPowerState;
        }
        else
        {
            _isOn = false;
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
            capturedResource.Info.ResourceType = Matter.Biomass;
            capturedResource.Info.Quantity = 0;
            capturedResource.RefreshLabel();
            capturedResource.SnapCrate(this, CrateAnchor.position);
        }

        if (capturedResource.Info.Quantity < MaximumBiomass)
        {
            capturedResource.Info.Quantity += BiomassCollected;
            BiomassCollected = 0;
        }
    }

    private Coroutine detachTimer;
    public void OnChildTriggerEnter(string childName, Collider c, ResourceComponent res)
    {
        if (detachTimer == null && res != null)
        {
            if (res.Info.ResourceType == Matter.Biomass)
            {
                capturedResource = res;
                capturedResource.SnapCrate(this, CrateAnchor.position);
            }
        }
    }

    public void DetachCrate(ResourceComponent detaching)
    {
        capturedResource = null;
        detachTimer = StartCoroutine(DetachTimer());
    }

    private IEnumerator DetachTimer()
    {
        yield return new WaitForSeconds(1f);
        detachTimer = null;
    }
}

