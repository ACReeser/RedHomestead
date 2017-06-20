using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.EVA;
using RedHomestead.Simulation;
using RedHomestead.Industry;
using RedHomestead.Electricity;

public class EVAStation : Converter, IPowerConsumer
{
    private ISink OxygenIn;

    public override float WattsConsumed
    {
        get
        {
            return EVA.PowerResupplyWattsPerSecond;
        }
    }
    
    public bool IsOn { get; set; }

    private void SyncMesh()
    {

    }

    public override void ClearHooks()
    {
        OxygenIn = null;
    }

    float oxygenBuffer;
    public override void Convert()
    {
        if (IsOn)
        {
            if (HasPower)
                SurvivalTimer.Instance.Power.Resupply(EVA.PowerResupplySeconds);

            if (OxygenIn != null)
            {
                if (oxygenBuffer < EVA.OxygenResupplyKilogramsPerUnit)
                    oxygenBuffer += OxygenIn.Get(Matter.Oxygen).Pull(EVA.OxygenResupplyKilogramsPerUnit);

                if (oxygenBuffer >= EVA.OxygenResupplyKilogramsPerUnit)
                {
                    SurvivalTimer.Instance.Oxygen.Resupply(EVA.OxygenResupplySeconds);
                    oxygenBuffer -= EVA.OxygenResupplyKilogramsPerUnit;
                }
            }
        }
    }

    public override Module GetModuleType()
    {
        return Module.EVAStation;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary();
    }

    public override void Report() { }

    public void ToggleUse(bool pumpingToEVA)
    {
        IsOn = pumpingToEVA;
    }

    public override void OnSinkConnected(ISink s)
    {
        if (s.HasContainerFor(Matter.Oxygen))
        {
            OxygenIn = s;
        }
    }

    public void OnEmergencyShutdown()
    {
        this.SyncMesh();
    }

    public override void OnPowerChanged()
    {
        this.SyncMesh();
    }
}
