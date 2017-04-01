using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using System.Linq;

public class RadioisotopeThermoelectricGenerator : ResourcelessGameplay, RedHomestead.Electricity.IPowerSupply
{
    public const float WattHoursGeneratedPerDay = _WattsGenerated * SunOrbit.MartianHoursPerDay;
    public const float _WattsGenerated = 130;

    public MeshFilter powerBacking { get; set; }
    public Transform powerMask { get; set; }
    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public bool VariablePowerSupply { get { return false; } }
    public float WattsGenerated
    {
        get
        {
            return _WattsGenerated;
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
