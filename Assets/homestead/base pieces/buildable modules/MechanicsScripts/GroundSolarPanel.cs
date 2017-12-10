using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundSolarPanel : SolarPanel
{
    void Awake()
    {
        Efficiency = MinimumEfficiency;
    }

    public override Module GetModuleType()
    {
        return Module.GroundSolarPanel;
    }
}