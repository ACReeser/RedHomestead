using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;

public class Electrolyzer : Converter, IPowerToggleable
{
    internal float OxygenPerSecond = .1f;
    internal float HydrogenPerSecond = .1f;
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

    private Sink HydrogenOut, OxygenOut, WaterIn;
    private bool IsFullyConnected
    {
        get
        {
            return HydrogenOut != null && OxygenOut != null && WaterIn != null;
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
                PushOxygenAndHydrogen();
            }
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        RefreshPowerSwitch();
    }

    private void PushOxygenAndHydrogen()
    {
        OxygenOut.Get(Matter.Oxygen).Push(OxygenPerSecond * Time.fixedDeltaTime);
        MatterHistory.Produce(Matter.Oxygen, OxygenPerSecond * Time.fixedDeltaTime);

        HydrogenOut.Get(Matter.Hydrogen).Push(HydrogenPerSecond * Time.fixedDeltaTime);
        MatterHistory.Produce(Matter.Hydrogen, HydrogenPerSecond * Time.fixedDeltaTime);
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
        HydrogenOut = OxygenOut = WaterIn = null;
    }

    public override void OnSinkConnected(Sink s)
    {
        if (s.HasContainerFor(Matter.Hydrogen))
        {
            HydrogenOut = s;
        }
        if (s.HasContainerFor(Matter.Oxygen))
        {
            OxygenOut = s;
        }
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
            "Water Electrolyzer",
            "1 kWh + 1kg H20 => .3kg O2 + .6kg H2",
            "100%",
            "100%",
            new ReportIOData() { Name = "Power", Flow = "1 kW/h", Amount = EnergyHistory[Energy.Electrical].Consumed + " kWh", Connected = HasPower },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Hydrogen", Flow = "1 kg/d", Amount = MatterHistory[Matter.Hydrogen].Consumed + " kg", Connected = HydrogenOut != null  }
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Methane", Flow = "1 kg/d", Amount = MatterHistory[Matter.Methane].Produced + " kg", Connected = OxygenOut != null },
                new ReportIOData() { Name = "Water", Flow = "1 kg/d", Amount = MatterHistory[Matter.Water].Produced + " kg", Connected = WaterIn != null }
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
    }
}
