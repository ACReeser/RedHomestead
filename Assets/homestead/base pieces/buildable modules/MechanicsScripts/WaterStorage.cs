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
        return new ResourceContainer(0f)
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
        float percentage = 0f;
        if (this.Data.Container != null)
        {
            percentage = this.Data.Container.UtilizationPercentage;
        }

        flowAmountRenderer.transform.localScale = new Vector3(1, percentage, 1);
    }
}
