using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using System;
using RedHomestead.Electricity;
using RedHomestead.Industry;

public class Electrolyzer : Converter, IPowerConsumerToggleable, IPowerConsumer
{
    internal float OxygenPerSecond = .03f;
    internal float HydrogenPerSecond = .06f;
    internal float WaterPerSecond = .1f;

    public MeshFilter powerCabinet;
    public MeshFilter PowerCabinet { get { return powerCabinet; } }

    public override float WattsConsumed
    {
        get
        {
            return 1f;
        }
    }

    private ISink HydrogenOut, OxygenOut, WaterIn;
    private bool IsFullyConnected
    {
        get
        {
            return HydrogenOut != null && OxygenOut != null && WaterIn != null;
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
        this.RefreshPowerSwitch();
    }

    private void PushOxygenAndHydrogen()
    {
        OxygenOut.Get(Matter.Oxygen).Push(OxygenPerSecond * Time.fixedDeltaTime);
        Data.MatterHistory.Produce(Matter.Oxygen, OxygenPerSecond * Time.fixedDeltaTime);

        HydrogenOut.Get(Matter.Hydrogen).Push(HydrogenPerSecond * Time.fixedDeltaTime);
        Data.MatterHistory.Produce(Matter.Hydrogen, HydrogenPerSecond * Time.fixedDeltaTime);
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

    public override void ClearSinks()
    {
        HydrogenOut = OxygenOut = WaterIn = null;
    }

    public override void OnSinkConnected(ISink s)
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
        GuiBridge.Instance.WriteReport(
            "Water Electrolyzer",
            "1 kWh + 1kg H20 => .3kg O2 + .6kg H2",
            "100%",
            "100%",
            new ReportIOData() { Name = "Power", Flow = "1 kW/h", Amount = Data.EnergyHistory[Energy.Electrical].Consumed + " kWh", Connected = HasPower },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Water", Flow = "1 kg/d", Amount = Data.MatterHistory[Matter.Water].Consumed + " kg", Connected = WaterIn != null  }
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Oxygen", Flow = ".3kg kg/d", Amount = Data.MatterHistory[Matter.Oxygen].Produced + " kg", Connected = OxygenOut != null },
                new ReportIOData() { Name = "Hydrogen", Flow = ".6kg kg/d", Amount = Data.MatterHistory[Matter.Hydrogen].Produced + " kg", Connected = HydrogenOut != null }
            }
            );
    }

    public override Module GetModuleType()
    {
        return Module.WaterElectrolyzer;
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
                Matter.Oxygen,  new ResourceContainer() {
                    MatterType = Matter.Oxygen,
                    TotalCapacity = 1f
                }
            },
            {
                Matter.Hydrogen,  new ResourceContainer() {
                    MatterType = Matter.Hydrogen,
                    TotalCapacity = 1f
                }
            }
        };
    }

    public void OnEmergencyShutdown()
    {
    }
}
