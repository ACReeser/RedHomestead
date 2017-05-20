using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Workshop : ResourcelessHabitatGameplay
{
    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public override Module GetModuleType()
    {
        return Module.Workshop;
    }

    public override void OnAdjacentChanged() { }

    public override void Report() {}

    public override void Tick() { }
}
