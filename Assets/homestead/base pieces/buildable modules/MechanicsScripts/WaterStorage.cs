using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;

public class WaterStorage : SingleResourceSink {

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }

    // Use this for initialization
    protected override void OnStart()
    {
        base.OnStart();
        this.SinkType = Matter.Water;
        this.Container = new ResourceContainer(StartAmount)
        {
            TotalCapacity = Capacity,
            MatterType = Matter.Water
        };
    }
    
    // Update is called once per frame
    void Update()
    {

    }
}
