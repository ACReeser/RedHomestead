using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.EVA;
using RedHomestead.Simulation;

public class EVAStation : Converter
{
    private const float EVAChargerPerTick = 7.5f;

    private bool InUse;
    private ISink OxygenIn;

    public override float WattsConsumed
    {
        get
        {
            return InUse ? EVA.PowerResupplyRatePerSecondWatts : 0f;
        }
    }

    public override void ClearHooks()
    {
        OxygenIn = null;
    }

    float oxygenBuffer;
    public override void Convert()
    {
        if (InUse)
        {
            if (HasPower)
                SurvivalTimer.Instance.Power.Resupply(EVA.PowerResupplyRatePerSecondWatts);

            if (OxygenIn != null)
            {
                oxygenBuffer = OxygenIn.Get(RedHomestead.Simulation.Matter.Oxygen).Pull(EVA.OxygenResupplyRatePerSecondKilograms / Matter.Oxygen.Kilograms());

                if (oxygenBuffer >= 1f)
                    SurvivalTimer.Instance.Oxygen.Resupply(EVA.OxygenResupplyRatePerSecondKilograms);
            }
        }
    }

    public override Module GetModuleType()
    {
        return Module.EVAStation;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return null;
    }

    public override void Report() { }

    public void ToggleUse(bool pumping)
    {
        InUse = pumping;
    }
}
