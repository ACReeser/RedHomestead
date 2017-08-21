using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableInterface : HabitatReadout {

    public Transform RationMeal, OrganicMeal, ShakeMeal;

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
        print("showing table contents");
        ResourceContainer cont = this.LinkedHab.Get(Matter.RationMeals);
        if (cont != null)
                RationMeal.gameObject.SetActive(cont.CurrentAmount > 0f);

        cont = this.LinkedHab.Get(Matter.OrganicMeals);
        if (cont != null)
                OrganicMeal.gameObject.SetActive(cont.CurrentAmount > 0f);

        cont = this.LinkedHab.Get(Matter.MealShakes);
        if (cont != null)
            ShakeMeal.gameObject.SetActive(cont.CurrentAmount > 0f);
    }
}
