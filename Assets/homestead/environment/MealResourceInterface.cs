using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class MealResourceInterface : HabitatResourceInterface {
    public Transform RationMeals, OrganicMeals, ShakeMeals, ShakePowders;
    
    protected override void OnResourceChange()
    {
        ShowPantry(RationMeals, LinkedHab.MatterTotals[Matter.RationMeal]);
        ShowPantry(OrganicMeals, LinkedHab.MatterTotals[Matter.OrganicMeal]);
        ShowPantry(ShakePowders, LinkedHab.MatterTotals[Matter.MealPowder]);
        ShowPantry(ShakeMeals, LinkedHab.MatterTotals[Matter.MealShake]);
    }

    private void ShowPantry(Transform visualizationRoot, SumContainer sumContainer)
    {
        for (int i = 0; i < visualizationRoot.childCount; i++)
        {
            visualizationRoot.GetChild(i).gameObject.SetActive(sumContainer.CurrentAmount >= i + 1);
        }
    }

    protected override void DoDisplay()
    {
        SumContainer bioC = LinkedHab.MatterTotals[Matter.Biomass];
        SumContainer organicC = LinkedHab.MatterTotals[Matter.OrganicMeal];
        SumContainer rationC = LinkedHab.MatterTotals[Matter.RationMeal];
        SumContainer powderC = LinkedHab.MatterTotals[Matter.MealPowder];
        SumContainer shakeC = LinkedHab.MatterTotals[Matter.MealShake];

        string days = (Math.Truncate(100 * ((bioC.CurrentAmount + organicC.CurrentAmount + rationC.CurrentAmount) / 2f + (powderC.CurrentAmount + shakeC.CurrentAmount) / 4f)) / 100).ToString();

        DisplayOut.text = string.Format("Meals: {0} day{9}\nBiomass:    {1}/{2}\nOrganic:  {3}/{4}\nRation:  {10}/{11}\nPowdered: {5}/{6}\nShakes:      {7}/{8}",
            days,    
            bioC.CurrentAmount,
            bioC.TotalCapacity,
            organicC.CurrentAmount,
            organicC.TotalCapacity,
            powderC.CurrentAmount,
            powderC.TotalCapacity,
            shakeC.CurrentAmount,
            shakeC.TotalCapacity,
            (days == "1") ? "" : "s",
            rationC.CurrentAmount,
            rationC.TotalCapacity
            );
    }
}
