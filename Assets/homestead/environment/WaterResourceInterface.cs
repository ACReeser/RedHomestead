using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;

public class WaterResourceInterface : HabitatResourceInterface {
    public List<Transform> Barrels;

    protected override void OnResourceChange(params Matter[] changedMatter)
    {
        int numBarrels = Mathf.CeilToInt(Barrels.Count * LinkedHab.Get(RedHomestead.Simulation.Matter.Water).UtilizationPercentage);
        
        for (int i = 0; i < Barrels.Count; i++)
        {
            Barrels[i].gameObject.SetActive(numBarrels >= i + 1);
        }

        base.OnResourceChange(changedMatter);
    }
}
