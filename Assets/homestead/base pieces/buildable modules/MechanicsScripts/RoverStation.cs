using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.EVA;
using RedHomestead.Simulation;
using RedHomestead.Industry;
using RedHomestead.Electricity;
using RedHomestead.Rovers;

public class RoverStation : Converter, IPowerConsumer
{
    private ISink OxygenIn, WaterIn;
    public SpriteRenderer Lung, Power, Water, Rover;

    public RoverInput AttachedRover;

    private bool HasOxygen
    {
        get { return OxygenIn != null; }
    }
    private bool HasWater
    {
        get { return WaterIn != null; }
    }
    private bool HasRover
    {
        get { return AttachedRover != null; }
    }

    public override float WattsConsumed
    {
        get
        {
            return EVA.PowerResupplyWattsPerSecond;
        }
    }
    
    public bool IsOn { get; set; }

    private void SyncStatusSprites()
    {
        Lung.color = HasOxygen ? ToggleTerminalStateData.Defaults.On : ToggleTerminalStateData.Defaults.Off;
        Power.color = HasPower ? ToggleTerminalStateData.Defaults.On : ToggleTerminalStateData.Defaults.Off;
        Rover.color = HasRover ? ToggleTerminalStateData.Defaults.On : ToggleTerminalStateData.Defaults.Off;
        Water.color = HasWater ? ToggleTerminalStateData.Defaults.On : ToggleTerminalStateData.Defaults.Off;
    }

    protected override void OnStart()
    {
        base.OnStart();
        this.SyncStatusSprites();
    }

    public override void ClearHooks()
    {
        OxygenIn = null;
        WaterIn = null;
        this.SyncStatusSprites();
    }
    
    private const float WaterPerTickUnits = .002f;
    private const float OxygenPerTickUnits = .002f;

    public override void Convert()
    {
        if (HasRover)
        {
            if (HasWater)
            {
                AttachedRover.Data.Oxygen.Push(WaterIn.Get(Matter.Water).Pull(WaterPerTickUnits));
            }

            if (HasOxygen)
            {
                AttachedRover.Data.Oxygen.Push(OxygenIn.Get(Matter.Oxygen).Pull(OxygenPerTickUnits));
            }
        }
    }

    public override Module GetModuleType()
    {
        return Module.RoverStation;
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
            this.SyncStatusSprites();
        }
        else if (s.HasContainerFor(Matter.Water))
        {
            WaterIn = s;
            this.SyncStatusSprites();
        }
    }

    public void OnEmergencyShutdown()
    {
        this.SyncStatusSprites();
    }

    public override void OnPowerChanged()
    {
        this.SyncStatusSprites();
    }

    internal void OnRoverAttachedChange(RoverInput rover)
    {
        this.AttachedRover = rover;
        this.SyncStatusSprites();
    }
}
