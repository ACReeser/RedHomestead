using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using System.Linq;

public class RadioisotopeThermoelectricGenerator : ResourcelessGameplay, RedHomestead.Electricity.IPowerSupply
{
    public const float WattHoursGeneratedPerDay = 3200f;

    public MeshFilter powerBacking { get; set; }
    public Transform powerMask { get; set; }
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
            return 130;
        }
    }

    public override Module GetModuleType()
    {
        return Module.RTG;
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
