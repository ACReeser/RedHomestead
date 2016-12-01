using UnityEngine;
using System.Collections;
using RedHomestead.Construction;

public class MealResourceInterface : HabitatResourceInterface {

    protected override void DoDisplay()
    {
        SumContainer bioC = LinkedHab.ComplexResourceTotals[Resource.Biomass];
        SumContainer prepC = LinkedHab.ComplexResourceTotals[Resource.PreparedMeal];
        SumContainer powderC = LinkedHab.ComplexResourceTotals[Resource.MealPowder];
        SumContainer shakeC = LinkedHab.ComplexResourceTotals[Resource.MealShake];

        string days = "1";

        DisplayOut.text = string.Format("Meals: {0}days\nBiomass: {1}/{2}\nPrepared: {3}/{4}\nPowdered: {5}/{6}\nShakes: {7}/{8}",
            days,    
            bioC.CurrentAmount,
            bioC.TotalCapacity,
            prepC.CurrentAmount,
            prepC.TotalCapacity,
            powderC.CurrentAmount,
            powderC.TotalCapacity,
            shakeC.CurrentAmount,
            shakeC.TotalCapacity
            );
    }
}
