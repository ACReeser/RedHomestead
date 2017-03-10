using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Buildings;

public class WaterStorage : SingleResourceModuleGameplay {

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    public override Module GetModuleType()
    {
        return Module.SmallWaterTank;
    }

    public override ResourceContainer GetStartingDataContainer()
    {
        return new ResourceContainer()
        {
            MatterType = Matter.Water,
            TotalCapacity = 10f,
        };
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

    // Update is called once per frame
    void Update()
    {

    }
}
