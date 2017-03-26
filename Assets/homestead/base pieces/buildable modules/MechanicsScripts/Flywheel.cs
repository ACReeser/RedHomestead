using RedHomestead.Electricity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Buildings;

public class Flywheel : ResourcelessGameplay, IBattery {
    private EnergyContainer e = new EnergyContainer(0)
    {
        TotalCapacity = 500
    };

    public EnergyContainer EnergyContainer
    {
        get
        {
            return e;
        }
    }

    public override float WattsConsumedPerTick
    {
        get
        {
            return 0;
        }
    }

    public float WattsGeneratedPerTick
    {
        get
        {
            return 0;
        }
    }

    public override Module GetModuleType()
    {
        return Module.Flywheel;
    }

    public override void OnAdjacentChanged()
    {
    }

    public override void Report()
    {
    }

    public override void Tick()
    {
    }
}
