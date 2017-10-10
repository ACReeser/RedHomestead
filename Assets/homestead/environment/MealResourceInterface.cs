using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class MealResourceInterface : HabitatResourceInterface {
    public Transform RationMeals, OrganicMeals, ShakeMeals, ShakePowders;
    
    protected override void OnResourceChange(params Matter[] changedMatter)
    {
        print("showing pantry contents");
        ShowPantry(RationMeals, LinkedHab.Get(Matter.RationMeals));
        ShowPantry(OrganicMeals, LinkedHab.Get(Matter.OrganicMeals));
        ShowPantry(ShakePowders, LinkedHab.Get(Matter.MealPowders));
        ShowPantry(ShakeMeals, LinkedHab.Get(Matter.MealShakes));
        Display();
    }

    private void ShowPantry(Transform visualizationRoot, ResourceContainer sumContainer)
    {
        float meals = sumContainer.CurrentAmount * sumContainer.MatterType.UnitsPerCubicMeter();
        for (int i = 0; i < visualizationRoot.childCount; i++)
        {
            visualizationRoot.GetChild(i).gameObject.SetActive(meals >= i + 1);
        }

        Collider c = visualizationRoot.GetComponent<Collider>();
        if (c != null)
            c.enabled = meals > 0f;
    }

    private void Display()
    {
        ResourceContainer bioC = LinkedHab.Get(Matter.Biomass);
        ResourceContainer organicC = LinkedHab.Get(Matter.OrganicMeals);
        ResourceContainer rationC = LinkedHab.Get(Matter.RationMeals);
        ResourceContainer powderC = LinkedHab.Get(Matter.MealPowders);
        ResourceContainer shakeC = LinkedHab.Get(Matter.MealShakes);

        float days = (
            (
                (
                    (bioC.CurrentAmount + organicC.CurrentAmount + rationC.CurrentAmount) * Matter.OrganicMeals.UnitsPerCubicMeter()
                ) / 2f
                +
                (
                    (powderC.CurrentAmount + shakeC.CurrentAmount) * Matter.MealShakes.UnitsPerCubicMeter()
                ) / 4f
            )
        );

        DisplayOut.text = string.Format("Meals: {0:0.##} day{9}\nBiomass:    {1:0.##}/{2:0.##}\nOrganic:  {3:0.##}/{4:0.##}\nRation:  {10:0.##}/{11:0.##}\nPowdered: {5:0.##}/{6:0.##}\nShakes:      {7:0.##}/{8:0.##}",
            days,    
            bioC.CurrentAmount * Matter.Biomass.UnitsPerCubicMeter(),
            bioC.TotalCapacity * Matter.Biomass.UnitsPerCubicMeter(),
            organicC.CurrentAmount * Matter.OrganicMeals.UnitsPerCubicMeter(),
            organicC.TotalCapacity * Matter.OrganicMeals.UnitsPerCubicMeter(),
            powderC.CurrentAmount * Matter.MealPowders.UnitsPerCubicMeter(),
            powderC.TotalCapacity * Matter.MealPowders.UnitsPerCubicMeter(),
            shakeC.CurrentAmount * Matter.MealShakes.UnitsPerCubicMeter(),
            shakeC.TotalCapacity * Matter.MealShakes.UnitsPerCubicMeter(),
            (days == 1) ? "" : "s",
            rationC.CurrentAmount * Matter.RationMeals.UnitsPerCubicMeter(),
            rationC.TotalCapacity * Matter.RationMeals.UnitsPerCubicMeter()
            );
    }

    void OnTriggerEnter(Collider other)
    {
        if (LinkedHab != null)
        {
            LinkedHab.ImportProduceToOrganicMeal(other.GetComponent<ResourceComponent>());
        }
    }
}
