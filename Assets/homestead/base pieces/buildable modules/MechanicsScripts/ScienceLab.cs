using RedHomestead.Buildings;
using RedHomestead.Equipment;
using RedHomestead.Persistence;
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

public class ScienceLab : ResourcelessHabitatGameplay, IEquipmentSwappable, IFlexDataContainer<ResourcelessModuleData, ScienceLabFlexData>
{
    public ScienceLabFlexData FlexData { get; set; }
    public Transform[] ToolsInLockers, lockers;

    public Transform[] Tools { get { return ToolsInLockers; } }
    public Transform[] Lockers { get { return lockers; } }
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
    }

    public void AcceptExperiment(IScienceExperiment experiment)
    {
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
                FlexData.CurrentBioExperiment = experiment as BiologyScienceExperiment;
                break;
            case ExperimentType.GeoSample:
                FlexData.CurrentGeoExperiment = experiment as GeologyScienceExperiment;
                break;
        }
        experiment.OnAccept();
    }

    public void CancelExperiment(IScienceExperiment experiment)
    {
        NullOutExperimentSlot(experiment);
        experiment.OnCancel();
    }

    private void NullOutExperimentSlot(IScienceExperiment experiment)
    {
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
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
        NullOutExperimentSlot(experiment);
        experiment.OnComplete();
    }
}
