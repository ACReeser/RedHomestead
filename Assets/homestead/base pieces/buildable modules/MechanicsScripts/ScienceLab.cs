﻿using RedHomestead.Buildings;
using RedHomestead.Equipment;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class ScienceLabFlexData
{
    public BiologyScienceExperiment CurrentBioExperiment;
    public GeologyScienceExperiment CurrentGeoExperiment;
}

public class ScienceLab : ResourcelessHabitatGameplay, IEquipmentSwappable, IFlexDataContainer<ResourcelessModuleData, ScienceLabFlexData>, ICrateSnapper
{
    public ScienceLabFlexData FlexData { get; set; }
    public Transform[] ToolsInLockers, lockers;
    public Transform MinilabPrefab, MinilabSnap;
    private Transform CurrentMinilab;
    public Transform[] Tools { get { return ToolsInLockers; } }
    public Transform[] Lockers { get { return lockers; } }

    internal static List<ScienceLab> ActiveLabs = new List<ScienceLab>();
    private Dictionary<Transform, Equipment> equipmentLockers = new Dictionary<Transform, Equipment>();

    private Equipment[] lockerEquipment = new Equipment[] {
        Equipment.Sampler,
        Equipment.GPS
    };
    public Equipment[] LockerEquipment { get { return lockerEquipment; } }
    public Dictionary<Transform, Equipment> EquipmentLockers { get { return equipmentLockers; } }

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public override Module GetModuleType()
    {
        return Module.Workshop;
    }

    public override void InitializeStartingData()
    {
        base.InitializeStartingData();
        this.FlexData = new ScienceLabFlexData();
    }

    public override void OnAdjacentChanged() { }

    public override void Report() { }

    public override void Tick() { }

    protected override void OnStart()
    {
        base.OnStart();
        this.InitializeSwappable();
        ScienceLab.ActiveLabs.Add(this);
    }

    public void OnDestroy()
    {
        ScienceLab.ActiveLabs.Remove(this);
    }

    public void AcceptExperiment(IScienceExperiment experiment)
    {
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
                FlexData.CurrentBioExperiment = experiment as BiologyScienceExperiment;
                if (CurrentMinilab == null)
                {
                    CreateMinilab();
                }
                break;
            case ExperimentType.GeoSample:
                FlexData.CurrentGeoExperiment = experiment as GeologyScienceExperiment;
                break;
        }
        experiment.OnAccept();
    }

    private void CreateMinilab()
    {
        CurrentMinilab = GameObject.Instantiate(MinilabPrefab);
        CurrentMinilab.rotation = MinilabSnap.rotation;
        CurrentMinilab.position = MinilabSnap.position;
        CurrentMinilab.GetComponent<Minilab>().Assign(this);
    }

    public void CancelExperiment(IScienceExperiment experiment)
    {
        CleanupExperiments(experiment);
        experiment.OnCancel();
    }

    private void CleanupExperiments(IScienceExperiment experiment)
    {
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
                if (CurrentMinilab != null)
                {
                    GameObject.Destroy(CurrentMinilab.gameObject);
                }
                FlexData.CurrentBioExperiment = null;
                break;
            case ExperimentType.GeoSample:
                FlexData.CurrentGeoExperiment = null;
                break;
        }
    }

    public void CompleteExperiment(IScienceExperiment experiment)
    {
        Science.Complete(experiment);
        CleanupExperiments(experiment);
        experiment.OnComplete();
    }

    internal void OnGeologySampleTaken(Deposit lastDeposit)
    {
        if (FlexData.CurrentGeoExperiment != null && FlexData.CurrentGeoExperiment.DepositID == lastDeposit.Data.DepositInstanceID)
        {
            FlexData.CurrentGeoExperiment.Progress = 1f;
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {

    }

    internal void Adopt(Minilab minilab)
    {
        this.CurrentMinilab = minilab.transform;
    }

}
