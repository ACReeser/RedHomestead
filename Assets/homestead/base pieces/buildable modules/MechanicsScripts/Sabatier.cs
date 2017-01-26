using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class Sabatier : Converter, IPowerToggleable
{
    internal float HydrogenPerSecond = .1f;
    internal float MethanePerSecond = .1f;
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

    private Sink HydrogenSource, MethaneOut, WaterOut;
    private bool IsFullyConnected
    {
        get
        {
            return HydrogenSource != null && MethaneOut != null && WaterOut != null;
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
            if (PullHydrogen())
            {
                PushMethaneAndWater();
            }
        }
    }

    protected override void OnStart()
    {
        base.OnStart();
        RefreshPowerSwitch();
    }

    private void PushMethaneAndWater()
    {
        MethaneOut.Get(Compound.Methane).Push(MethanePerSecond * Time.fixedDeltaTime);
        CompoundHistory.Produce(Compound.Methane, MethanePerSecond * Time.fixedDeltaTime);
        WaterOut.Get(Compound.Water).Push(WaterPerSecond * Time.fixedDeltaTime);
        CompoundHistory.Produce(Compound.Water, MethanePerSecond * Time.fixedDeltaTime);
    }

    private float hydrogenBuffer = 0f;
    private bool PullHydrogen()
    {
        if (HydrogenSource != null)
        {
            float newHydrogen = HydrogenSource.Get(Compound.Hydrogen).Pull(HydrogenPerSecond * Time.fixedDeltaTime);
            hydrogenBuffer += newHydrogen;
            CompoundHistory.Consume(Compound.Hydrogen, newHydrogen);

            float hydrogenThisTick = HydrogenPerSecond * Time.fixedDeltaTime;

            if (hydrogenBuffer >= hydrogenThisTick)
            {
                hydrogenBuffer -= hydrogenThisTick;
                return true;
            }
        }

        return false;
    }

    public override void ClearHooks()
    {
        HydrogenSource = MethaneOut = WaterOut = null;
    }

    public override void OnSinkConnected(Sink s)
    {
        if (s.HasContainerFor(Compound.Hydrogen))
        {
            HydrogenSource = s;
        }
        if (s.HasContainerFor(Compound.Methane))
        {
            MethaneOut = s;
        }
        if (s.HasContainerFor(Compound.Water))
        {
            WaterOut = s;
        }
    }

    public override void Report()
    {
        //todo: report v3: flow is "0/1 kWh" etc, flow and amount update over time
        //using some sort of UpdateReport() call (which reuses built text boxes)
        //todo: report v4: each row gets a graph over time that shows effciency or flow
        //print(String.Format("HasPower: {3} - Hydrogen in: {0} - Water out: {1} - Methane out: {2}", CompoundHistory[Compound.Hydrogen].Consumed, CompoundHistory[Compound.Water].Produced, CompoundHistory[Compound.Methane].Produced, HasPower));
        GuiBridge.Instance.WriteReport(
            "Sabatier Reactor",
            "1 kWh + 1kg H2 => 1kg CH4 + 1kg H2O",
            "100%",
            "100%",
            new ReportIOData() { Name = "Power", Flow = "1 kW/h", Amount = EnergyHistory[Energy.Electrical].Consumed + " kWh", Connected = HasPower },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Hydrogen", Flow = "1 kg/d", Amount = CompoundHistory[Compound.Hydrogen].Consumed + " kg", Connected = HydrogenSource != null  }
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Methane", Flow = "1 kg/d", Amount = CompoundHistory[Compound.Methane].Produced + " kg", Connected = MethaneOut != null },
                new ReportIOData() { Name = "Water", Flow = "1 kg/d", Amount = CompoundHistory[Compound.Water].Produced + " kg", Connected = WaterOut != null }
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
