using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class WaterStorage : SingleResourceSink {
    public float StartAmount, Capacity;

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    // Use this for initialization
    void Start()
    {
        this.SinkType = Compound.Water;
        this.Container = new ResourceContainer(StartAmount)
        {
            TotalCapacity = Capacity,
            SimpleCompoundType = Compound.Water
        };
    }
    
    // Update is called once per frame
    void Update()
    {

    }
}
