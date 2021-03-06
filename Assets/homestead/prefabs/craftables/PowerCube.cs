﻿using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using UnityEngine;
using RedHomestead.Persistence;
using RedHomestead.Electricity;

[Serializable]
public class PowerCubeData : FacingData
{
    public EnergyContainer EnergyContainer;
    public string PowerableInstanceID;
    public float FaultedPercentage;
}

public class PowerCube : MovableSnappable, IDataContainer<PowerCubeData>, IBattery {
    private PowerCubeData data;
    public PowerCubeData Data { get { return data; } set { data = value; } }

    public string PowerGridInstanceID { get; set; }
    public string PowerableInstanceID { get { return data.PowerableInstanceID; } }

    public PowerVisualization _powerViz;
    public PowerVisualization PowerViz { get { return _powerViz; } }
    public bool CanMalfunction { get { return !String.IsNullOrEmpty(PowerGridInstanceID); } }

    public FailureAnchors failureEffectAnchors;
    public FailureAnchors FailureEffectAnchors { get { return failureEffectAnchors; } }
    public float FaultedPercentage { get { return data.FaultedPercentage; } set { data.FaultedPercentage = value; } }

    public override string GetText()
    {
        return "Battery";
    }

    // Use this for initialization
    void Start () {
        if (this.data == null)
            this.data = new PowerCubeData()
            {
                EnergyContainer = new EnergyContainer(0f)
                {
                    TotalCapacity = ElectricityConstants.WattHoursPerBatteryBlock
                },
                PowerableInstanceID = Guid.NewGuid().ToString()
            };

        this.InitializePowerVisualization();
    }

    public EnergyContainer EnergyContainer
    {
        get
        {
            return this.data.EnergyContainer;
        }
    }

    public override void OnPickedUp()
    {
        base.OnPickedUp();

        if (FlowManager.Instance.PowerGrids.Edges.ContainsKey(this) && FlowManager.Instance.PowerGrids.Edges[this].Count > 0)
        {
            FlowManager.Instance.PowerGrids.Detach(this);
        }
    }

    public void Hide()
    {
        this.ShowHide(false);
    }

    public void Show()
    {
        this.ShowHide(true);
    }

    private void ShowHide(bool state)
    {
        this._powerViz.PowerActive.gameObject.SetActive(state);
        this._powerViz.PowerBacking.gameObject.SetActive(state);
        this._powerViz.PowerMask.gameObject.SetActive(state);

        int socketCount = 1;
        for (int i = this.transform.childCount - 1; i > -1; i--)
        {
            if (socketCount > 4)
                break;

            this.transform.GetChild(i).gameObject.SetActive(state);
            socketCount++;
        }
    }
}
