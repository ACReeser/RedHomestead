using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Electricity;

public class SolarPanel : ResourcelessGameplay, IPowerSupply
{
    public float Efficiency = .22f;
    public float GrossSolarWattagePerTick = 20f;
    public MeshFilter powerBacking { get; set; }
    public Transform powerMask { get; set; }

    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public bool VariablePowerSupply { get { return true; } }
    public float WattsGenerated
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

    public override void InitializeStartingData()
    {
        this.Data = new ResourcelessModuleData()
        {
            ModuleInstanceID = Guid.NewGuid().ToString(),
            ModuleType = GetModuleType()
        };
    }

    public override Module GetModuleType()
    {
        return Module.SolarPanelSmall;
    }
}
