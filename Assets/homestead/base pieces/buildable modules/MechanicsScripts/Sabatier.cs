using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class Sabatier : MultipleResourceConverter
{
    internal float HydrogenPerTick = 1f;
    internal float MethanePerTick = 1f;
    internal float WaterPerTick = 1f;

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
        MethaneOut.Get(Compound.Methane).Push(MethanePerTick);
        CompoundHistory.Consume(Compound.Methane, MethanePerTick);
        WaterOut.Get(Compound.Water).Push(WaterPerTick);
        CompoundHistory.Consume(Compound.Water, MethanePerTick);
    }

    private float hydrogenBuffer = 0f;
    private bool PullHydrogen()
    {
        if (HydrogenSource != null)
        {
            float newHydrogen = HydrogenSource.Get(Compound.Hydrogen).Pull(HydrogenPerTick);
            hydrogenBuffer += newHydrogen;
            CompoundHistory.Consume(Compound.Hydrogen, newHydrogen);

            if (hydrogenBuffer >= HydrogenPerTick)
                return true;
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
}
