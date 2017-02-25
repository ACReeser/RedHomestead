using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;

public class AlgaeTank : Converter, IPowerToggleable
{
    public AudioClip HandleChangeClip;

    internal float BiomassPerSecond = .1f;
    internal float WaterPerSecond = .1f;
    internal bool _isOn = false;

    public MeshFilter PowerCabinet;
    public Mesh OnMesh, OffMesh;

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

    public override void Convert()
    {
        if (HasPower && IsOn && IsFullyConnected)
        {
            if (PullWater())
            {
                PushBiomass();
            }
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        RefreshPowerSwitch();
    }

    private void PushBiomass()
    {
        //MethaneOut.Get(Matter.Methane).Push(MethanePerSecond * Time.fixedDeltaTime);
        //MatterHistory.Produce(Matter.Methane, MethanePerSecond * Time.fixedDeltaTime);
        //WaterOut.Get(Matter.Water).Push(WaterPerSecond * Time.fixedDeltaTime);
        //MatterHistory.Produce(Matter.Water, MethanePerSecond * Time.fixedDeltaTime);
    }

    private float waterBuffer = 0f;
    private bool PullWater()
    {
        //if (HydrogenSource != null)
        //{
        //    float newHydrogen = HydrogenSource.Get(Matter.Hydrogen).Pull(HydrogenPerSecond * Time.fixedDeltaTime);
        //    hydrogenBuffer += newHydrogen;
        //    MatterHistory.Consume(Matter.Hydrogen, newHydrogen);

        //    float hydrogenThisTick = HydrogenPerSecond * Time.fixedDeltaTime;

        //    if (hydrogenBuffer >= hydrogenThisTick)
        //    {
        //        hydrogenBuffer -= hydrogenThisTick;
        //        return true;
        //    }
        //}

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
            "1 kWh + 1kg H2O => 1kg Biomass",
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
}

