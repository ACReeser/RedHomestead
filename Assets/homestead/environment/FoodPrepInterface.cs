using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class FoodPrepInterface : HabitatModule
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

        float meals = container.CurrentAmount * container.MatterType.MealsPerCubicMeter();

        for (int i = 0; i < BiomassStorageRoot.childCount; i++)
        {
            BiomassStorageRoot.GetChild(i).gameObject.SetActive(meals >= i + 1);
        }

        BiomassVisualization.gameObject.SetActive(meals > 0f);

        container = this.LinkedHab.Get(Matter.MealPowder);

        float powders = container.CurrentAmount * container.MatterType.MealsPerCubicMeter();
        this.PowderVisualization.gameObject.SetActive(powders > 0f);
    }
}
