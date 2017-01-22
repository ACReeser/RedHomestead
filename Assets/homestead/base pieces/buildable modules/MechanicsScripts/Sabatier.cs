using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class Sabatier : MultipleResourceConverter
{
    internal float HydrogenPerSecond = .1f;
    internal float MethanePerSecond = .1f;
    internal float WaterPerSecond = .1f;

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

    public override void Convert()
    {
        if (HasPower && IsFullyConnected)
        {
            if (PullHydrogen())
            {
                PushMethaneAndWater();
            }
        }
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
        //print(String.Format("HasPower: {3} - Hydrogen in: {0} - Water out: {1} - Methane out: {2}", CompoundHistory[Compound.Hydrogen].Consumed, CompoundHistory[Compound.Water].Produced, CompoundHistory[Compound.Methane].Produced, HasPower));
        GuiBridge.Instance.WriteReport(
            "Sabatier Reactor",
            "1 kWh + 1kg H2 => 1kg CH4 + 1kg H2O",
            "100%",
            "100%",
            new ReportIOData() { Name = "Power", Now = EnergyHistory[Energy.Electrical].Consumed + " kWh", AllTime = EnergyHistory[Energy.Electrical].Consumed + " kWh" },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Hydrogen", Now = CompoundHistory[Compound.Hydrogen].Consumed + " kg", AllTime = CompoundHistory[Compound.Hydrogen].Consumed + " kg" }
            },
            new ReportIOData[]
            {
                new ReportIOData() { Name = "Methane", Now = CompoundHistory[Compound.Methane].Produced + " kg", AllTime = CompoundHistory[Compound.Methane].Produced + " kg" },
                new ReportIOData() { Name = "Water", Now = CompoundHistory[Compound.Water].Produced + " kg", AllTime = CompoundHistory[Compound.Water].Produced + " kg" }
            }
            );
    }
}
