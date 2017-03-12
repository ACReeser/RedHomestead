using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableInterface : HabitatModule {

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
        ResourceContainer cont = this.LinkedHab.Get(Matter.RationMeal);
        if (cont != null)
                RationMeal.gameObject.SetActive(cont.CurrentAmount > 0f);

        cont = this.LinkedHab.Get(Matter.OrganicMeal);
        if (cont != null)
                OrganicMeal.gameObject.SetActive(cont.CurrentAmount > 0f);

        cont = this.LinkedHab.Get(Matter.MealShake);
        if (cont != null)
            ShakeMeal.gameObject.SetActive(cont.CurrentAmount > 0f);
    }
}
