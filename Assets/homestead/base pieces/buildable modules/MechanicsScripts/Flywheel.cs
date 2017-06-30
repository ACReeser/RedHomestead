using RedHomestead.Electricity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Buildings;
using RedHomestead.Simulation;

public class Flywheel : ResourcelessGameplay, IBattery {
    private EnergyContainer e = new EnergyContainer(0)
    {
        TotalCapacity = RadioisotopeThermoelectricGenerator.WattHoursGeneratedPerDay / 2
    };

    public EnergyContainer EnergyContainer
    {
        get
        {
            return e;
        }
    }

    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public float WattsGenerated
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
