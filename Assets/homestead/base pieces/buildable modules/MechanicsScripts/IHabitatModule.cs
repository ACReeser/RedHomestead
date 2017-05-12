using RedHomestead.Buildings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHabitatModule: IBuildable
{
    Habitat LinkedHabitat { get; }
    List<IHabitatModule> AdjacentModules { get; }
    void AddAdjacent(IHabitatModule adjacent);
    void SetHabitat(Habitat habitat);
}

