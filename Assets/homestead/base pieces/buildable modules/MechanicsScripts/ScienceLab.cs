using RedHomestead.Buildings;
using RedHomestead.Equipment;
using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExperimentType { GeoSample, BioMinilab }
public enum ExperimentStatus { Available, Accepted, Completed }

public static class ExperimentExtensions
{
    public static string Description(this ExperimentType type)
    {
        switch (type)
        {
            case ExperimentType.BioMinilab:
                return "Place a mini-greenhouse\nand wait for it to generate data";
            default:
            case ExperimentType.GeoSample:
                return "Sample the regolith\nin a deposit and return the data";
        }
    }
    public static string Title(this IScienceExperiment experiment)
    {
        string result = "";
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
                result = "MINILAB";
                break;
            default:
            case ExperimentType.GeoSample:
                result = "DEPOSIT SAMPLE";
                break;
        }

        return result + " MISSION " + experiment.MissionNumber;
    }

    public static ExperimentStatus Status(this IScienceExperiment experiment)
    {
        if (experiment.Progress < 0)
        {
            return ExperimentStatus.Available;
        }
        else if (experiment.Progress >= experiment.DurationDays)
        {
            return ExperimentStatus.Completed;
        }
        else
        {
            return ExperimentStatus.Accepted;
        }
    }
}

public interface IScienceExperiment
{
    ExperimentType Experiment { get; }
    float Progress { get; }
    int Reward { get; }
    int DurationDays { get; }
    int MissionNumber { get; }
}

[Serializable]
public class BiologyScienceExperiment: IScienceExperiment
{
    public float Progress { get; set; }
    public Vector3 TargetPosition;
    public int DurationDays {get; set; }
    public int Reward { get; set; }
    public int MissionNumber { get; set; }
    public ExperimentType Experiment { get { return ExperimentType.BioMinilab; } }
}

[Serializable]
public class GeologyScienceExperiment : IScienceExperiment
{
    public string DepositID;
    public float Progress { get; set; }
    public int Reward { get; set; }
    public int DurationDays { get; set; }
    public int MissionNumber { get; set; }
    public ExperimentType Experiment { get { return ExperimentType.GeoSample; } }
}

[Serializable]
public class ScienceLabFlexData
{
    public BiologyScienceExperiment BioExperiment;
    public GeologyScienceExperiment GeoExperiment;
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
}
