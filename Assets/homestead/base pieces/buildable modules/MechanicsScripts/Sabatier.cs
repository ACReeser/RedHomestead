using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Electricity;

public class Sabatier : Converter, IPowerToggleable, IPowerConsumer
{
    public AudioClip HandleChangeClip;

    internal float HydrogenPerSecond = .1f;
    internal float MethanePerSecond = .1f;
    internal float WaterPerSecond = .1f;
    public bool IsOn { get; set; }

    public MeshFilter PowerCabinet;
    public Mesh OnMesh, OffMesh;

    public override float WattsConsumedPerTick
    {
        get
        {
            return 1f;
        }
    }

    private ISink HydrogenSource, MethaneOut, WaterOut;
    private bool IsFullyConnected
    {
        get
        {
            return HydrogenSource != null && MethaneOut != null && WaterOut != null;
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
        MethaneOut.Get(Matter.Methane).Push(MethanePerSecond * Time.fixedDeltaTime);
        Data.MatterHistory.Produce(Matter.Methane, MethanePerSecond * Time.fixedDeltaTime);
        WaterOut.Get(Matter.Water).Push(WaterPerSecond * Time.fixedDeltaTime);
        Data.MatterHistory.Produce(Matter.Water, MethanePerSecond * Time.fixedDeltaTime);
    }

    private float hydrogenBuffer = 0f;
    private bool PullHydrogen()
    {
        if (HydrogenSource != null)
        {
            float newHydrogen = HydrogenSource.Get(Matter.Hydrogen).Pull(HydrogenPerSecond * Time.fixedDeltaTime);
            hydrogenBuffer += newHydrogen;
            Data.MatterHistory.Consume(Matter.Hydrogen, newHydrogen);

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

    public override void OnSinkConnected(ISink s)
    {
        if (s.HasContainerFor(Matter.Hydrogen))
        {
            HydrogenSource = s;
        }
        if (s.HasContainerFor(Matter.Methane))
        {
            MethaneOut = s;
        }
        if (s.HasContainerFor(Matter.Water))
        {
            WaterOut = s;
        }
    }

    public override void Report()
    {
        //todo: report v3: flow is "0/1 kWh" etc, flow and amount update over time
        //using some sort of UpdateReport() call (which reuses built text boxes)
        //todo: report v4: each row gets a graph over time that shows effciency or flow
        //print(String.Format("HasPower: {3} - Hydrogen in: {0} - Water out: {1} - Methane out: {2}", MatterHistory[Matter.Hydrogen].Consumed, MatterHistory[Matter.Water].Produced, MatterHistory[Matter.Methane].Produced, HasPower));
        GuiBridge.Instance.WriteReport(
            "Sabatier Reactor",
            "1 kWh + 1kg H2 => 1kg CH4 + 1kg H2O",
            "100%",
            "100%",
            new ReportIOData() { Name = "Power", Flow = "1 kW/h", Amount = Data.EnergyHistory[Energy.Electrical].Consumed + " kWh", Connected = HasPower },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Hydrogen", Flow = "1 kg/d", Amount = Data.MatterHistory[Matter.Hydrogen].Consumed + " kg", Connected = HydrogenSource != null  }
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Methane", Flow = "1 kg/d", Amount = Data.MatterHistory[Matter.Methane].Produced + " kg", Connected = MethaneOut != null },
                new ReportIOData() { Name = "Water", Flow = "1 kg/d", Amount = Data.MatterHistory[Matter.Water].Produced + " kg", Connected = WaterOut != null }
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

    public override Module GetModuleType()
    {
        return Module.SabatierReactor;
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
                Matter.Hydrogen,  new ResourceContainer() {
                    MatterType = Matter.Hydrogen,
                    TotalCapacity = 1f
                }
            },
            {
                Matter.Methane,  new ResourceContainer() {
                    MatterType = Matter.Methane,
                    TotalCapacity = 1f
                }
            }
        };
    }

    public void OnEmergencyShutdown()
    {
    }
}
