using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;

public class Greenhouse : Converter, IHabitatModule
{
    public List<IHabitatModule> AdjacentModules { get; set; }

    public Habitat LinkedHabitat { get; set; }

    public override float WattsConsumed
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override void ClearHooks()
    {
    }

    public override void Convert()
    {
    }

    public override Module GetModuleType()
    {
        return Module.GreenhouseHall;
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        return new ResourceContainerDictionary()
        {

        };
    }

    public override void Report()
    {
    }
}
