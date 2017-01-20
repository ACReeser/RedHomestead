using UnityEngine;
using System.Collections;
using System;

public class SolarPanel : PowerSupply
{
    public float Efficiency = .22f;
    public float GrossSolarWattagePerTick = 20f;

    void Awake()
    {
        HasPower = true;
    }

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    public override float WattsPerTick
    {
        get
        {
            return GrossSolarWattagePerTick * Efficiency;
        }
    }

    public override void OnAdjacentChanged()
    {
        
    }

    public override void Tick()
    {
        
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }
}
