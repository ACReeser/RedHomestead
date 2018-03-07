using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.Electricity;

public class JunctionBox : ResourcelessGameplay, IPowerConsumer
{
    public bool IsOn { get; set; }

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public override Module GetModuleType()
    {
        return Module.JunctionBox;
    }

    public override void OnAdjacentChanged()
    {
    }

    public void OnEmergencyShutdown()
    {
    }

    public override void Report()
    {
    }

    public override void Tick()
    {
    }
}
