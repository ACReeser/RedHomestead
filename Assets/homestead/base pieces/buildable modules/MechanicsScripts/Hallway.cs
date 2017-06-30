using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;

public class Hallway : GenericBaseModule
{
    public override Module GetModuleType()
    {
        return Module.HallwayNode;
    }
}