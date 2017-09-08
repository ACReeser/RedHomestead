using RedHomestead.Electricity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Buildings;
using System;
using RedHomestead.Industry;
using RedHomestead.Simulation;

public class AirMiner : Converter, IPowerConsumerToggleable
{
    private ResourceContainer co2Out;
    private const float co2PerTickUnits = 0.01f;

    public MeshFilter powerCabinet;
    public MeshFilter PowerCabinet { get { return powerCabinet; } }


    public override float WattsConsumed
    {
        get
        {
            return ElectricityConstants.WattsPerBlock;
        }
    }

    public override void ClearSinks()
    {
        co2Out = null;
    }

    public override void Convert()
    {
        if (HasPower && IsOn)
        {
            if (co2Out != null)
            {
                co2Out.Push(co2PerTickUnits);
            }
            else if (Data.Containers[Matter.CarbonDioxide].AvailableCapacity > 0f)
            {
                Data.Containers[Matter.CarbonDioxide].Push(co2PerTickUnits);
            }
        }
    }

    public override Module GetModuleType()
    {
        return Module.AirMiner;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary()
        {
            { Matter.CarbonDioxide, new ResourceContainer(Matter.CarbonDioxide, 0f) }
        };
    }

    public void OnEmergencyShutdown()
    {
    }

    public override void Report()
    {
    }

    public override void OnSinkConnected(ISink s)
    {
        if (s.HasContainerFor(RedHomestead.Simulation.Matter.CarbonDioxide))
        {
            co2Out = s.Get(RedHomestead.Simulation.Matter.CarbonDioxide);
        }
    }
    
    protected override void OnStart()
    {
        base.OnStart();
        this.RefreshPowerSwitch();
    }
}
