﻿using UnityEngine;
using System.Collections;
using RedHomestead.Construction;
using System;

public class HabitatResourceInterface : MonoBehaviour {
    public Habitat LinkedHab;

    public TextMesh DisplayOut;
    public Resource DisplayResource;
    public Compound DisplayCompound = Compound.Unspecified;
    public string HeaderText = "";

    private bool useResource;
	// Use this for initialization
	void Start () {
        if (LinkedHab == null)
            LinkedHab = transform.parent.GetComponent<Habitat>();

        if (LinkedHab == null)
        {
            UnityEngine.Debug.LogWarning("Hab resource interface not linked!");
            this.enabled = false;
        }
        useResource = (DisplayCompound == Compound.Unspecified);
	}
	
	void FixedUpdate ()
    {
        DoDisplay();
    }

    protected virtual void DoDisplay()
    {
        DisplaySingleContainer();
    }

    private void DisplaySingleContainer()
    {
        SumContainer container = null;

        if (useResource)
        {
            container = LinkedHab.ComplexResourceTotals[DisplayResource];
        }
        else
        {
            container = LinkedHab.BasicResourceTotals[DisplayCompound];
        }

        DisplayOut.text = string.Format("{0}: {1}% {2}/{3}\n{4}{5}/{6}",
            HeaderText,
            container.UtilizationPercentageString(),
            container.CurrentAmount,
            container.TotalCapacity,
            container.LastTickRateOfChange > 0 ? "+" : "-",
            container.LastTickRateOfChange,
            "g/s" //todo: make human readable with large/small quants and scales
            );
    }
}
