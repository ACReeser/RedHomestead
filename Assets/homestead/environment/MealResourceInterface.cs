using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class MealResourceInterface : HabitatResourceInterface {
    public Transform RationMeals, OrganicMeals, ShakeMeals, ShakePowders;
    
    protected override void OnResourceChange(params Matter[] changedMatter)
    {
        print("showing pantry contents");
        ShowPantry(RationMeals, LinkedHab.Get(Matter.RationMeal));
        ShowPantry(OrganicMeals, LinkedHab.Get(Matter.OrganicMeal));
        ShowPantry(ShakePowders, LinkedHab.Get(Matter.MealPowder));
        ShowPantry(ShakeMeals, LinkedHab.Get(Matter.MealShake));
        Display();
    }

    private void ShowPantry(Transform visualizationRoot, ResourceContainer sumContainer)
    {
        float meals = sumContainer.CurrentAmount * sumContainer.MatterType.MealsPerCubicMeter();
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
        ResourceContainer organicC = LinkedHab.Get(Matter.OrganicMeal);
        ResourceContainer rationC = LinkedHab.Get(Matter.RationMeal);
        ResourceContainer powderC = LinkedHab.Get(Matter.MealPowder);
        ResourceContainer shakeC = LinkedHab.Get(Matter.MealShake);

        float days = (
            (
                (
                    (bioC.CurrentAmount + organicC.CurrentAmount + rationC.CurrentAmount) * Matter.OrganicMeal.MealsPerCubicMeter()
                ) / 2f
                +
                (
                    (powderC.CurrentAmount + shakeC.CurrentAmount) * Matter.MealShake.MealsPerCubicMeter()
                ) / 4f
            )
        );

        DisplayOut.text = string.Format("Meals: {0:0.##} day{9}\nBiomass:    {1:0.##}/{2:0.##}\nOrganic:  {3:0.##}/{4:0.##}\nRation:  {10:0.##}/{11:0.##}\nPowdered: {5:0.##}/{6:0.##}\nShakes:      {7:0.##}/{8:0.##}",
            days,    
            bioC.CurrentAmount * Matter.Biomass.MealsPerCubicMeter(),
            bioC.TotalCapacity * Matter.Biomass.MealsPerCubicMeter(),
            organicC.CurrentAmount * Matter.OrganicMeal.MealsPerCubicMeter(),
            organicC.TotalCapacity * Matter.OrganicMeal.MealsPerCubicMeter(),
            powderC.CurrentAmount * Matter.MealPowder.MealsPerCubicMeter(),
            powderC.TotalCapacity * Matter.MealPowder.MealsPerCubicMeter(),
            shakeC.CurrentAmount * Matter.MealShake.MealsPerCubicMeter(),
            shakeC.TotalCapacity * Matter.MealShake.MealsPerCubicMeter(),
            (days == 1) ? "" : "s",
            rationC.CurrentAmount * Matter.RationMeal.MealsPerCubicMeter(),
            rationC.TotalCapacity * Matter.RationMeal.MealsPerCubicMeter()
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
