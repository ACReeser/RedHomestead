using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class HabitatResourceInterface : HabitatModule
{
    public TextMesh DisplayOut;
    public Matter DisplayResource;
    public Matter DisplayCompound = Matter.Unspecified;
    public string HeaderText = "";

    private bool useResource;

	// Use this for initialization
	protected override void OnStart () {
        if (LinkedHab != null)
        {
            //todo- change to event-based
            OnResourceChange();
        }

        useResource = (DisplayCompound == Matter.Unspecified);
	}
	
	void FixedUpdate()
    {
        DoDisplay();
    }

    protected virtual void DoDisplay()
    {
        DisplaySingleContainer();
    }

    protected virtual void OnResourceChange()
    {
    }

    private void DisplaySingleContainer()
    {
        SumContainer container = LinkedHab.MatterTotals[DisplayResource];

        DisplayOut.text = string.Format("{0}: {1}\n{2}/{3}kg\n{4}{5} {6}",
            HeaderText,
            container.UtilizationPercentageString(),
            container.CurrentAmount,
            container.TotalCapacity,
            container.LastTickRateOfChange == 0 ? " " : container.LastTickRateOfChange > 0 ? "+" : "-",
            container.LastTickRateOfChange,
            "g/s" //todo: make human readable with large/small quants and scales
            );
    }
}
