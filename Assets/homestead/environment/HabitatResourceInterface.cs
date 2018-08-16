using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class HabitatResourceInterface : HabitatReadout
{
    public TextMesh DisplayOut;
    public Matter DisplayResource;
    public string HeaderText = "";
    
	// Use this for initialization
	protected override void OnStart () {
        if (LinkedHab != null)
        {
            LinkedHab.OnResourceChange += OnResourceChange;
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
            container.CurrentAmount * container.MatterType.Kilograms(),
            container.TotalCapacity * container.MatterType.Kilograms()
            //container.LastTickRateOfChange == 0 ? " " : container.LastTickRateOfChange > 0 ? "+" : "-",
            //container.LastTickRateOfChange,
            //"g/s" //todo: make human readable with large/small quants and scales
            );
    }

    void OnDestroy()
    {
        this._OnDestroy();
    }

    protected virtual void _OnDestroy()
    {
        if (this.LinkedHab != null)
        {
            LinkedHab.OnResourceChange -= this.OnResourceChange;
        }
    }
}
