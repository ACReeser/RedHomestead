using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class HabitatResourceInterface : HabitatModule
{
    public TextMesh DisplayOut;
    public Matter DisplayResource;
    public string HeaderText = "";
    
	// Use this for initialization
	protected override void OnStart () {
        if (LinkedHab != null)
        {
            LinkedHab.OnResourceChange += OnResourceChange;
            //todo- change to event-based
            OnResourceChange();
        }
	}

    protected virtual void OnResourceChange(params Matter[] changedMatter)
    {
        DisplaySingleContainer();
    }

    private void DisplaySingleContainer()
    {
        ResourceContainer container = LinkedHab.Get(DisplayResource);

        DisplayOut.text = string.Format("{0}: {1}\n{2}/{3}kg",
            HeaderText,
            container.UtilizationPercentageString(),
            container.CurrentAmount,
            container.TotalCapacity
            //container.LastTickRateOfChange == 0 ? " " : container.LastTickRateOfChange > 0 ? "+" : "-",
            //container.LastTickRateOfChange,
            //"g/s" //todo: make human readable with large/small quants and scales
            );
    }
}
