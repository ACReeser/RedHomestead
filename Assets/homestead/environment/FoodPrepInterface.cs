using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class FoodPrepInterface : HabitatReadout
{
    public Transform PowderVisualization, BiomassVisualization, BiomassStorageRoot;

    protected override void OnStart()
    {
        if (this.LinkedHab != null)
        {
            this.LinkedHab.OnResourceChange += this.OnResourceChange;
            this.OnResourceChange();
        }
    }

    private void OnResourceChange(params Matter[] type)
    {
        ResourceContainer container = this.LinkedHab.Get(Matter.Biomass);

        float meals = container.CurrentAmount * container.MatterType.UnitsPerCubicMeter();

        for (int i = 0; i < BiomassStorageRoot.childCount; i++)
        {
            BiomassStorageRoot.GetChild(i).gameObject.SetActive(meals >= i + 1);
        }

        BiomassVisualization.gameObject.SetActive(meals > 0f);

        container = this.LinkedHab.Get(Matter.MealPowders);

        float powders = container.CurrentAmount * container.MatterType.UnitsPerCubicMeter();
        this.PowderVisualization.gameObject.SetActive(powders > 0f);
    }
}
