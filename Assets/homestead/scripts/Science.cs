using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    public static Sprite Sprite(this ExperimentType type)
    {
        return IconAtlas.Instance.ScienceExperimentIcons[Convert.ToInt32(type)];
    }

    public static string Title(this IScienceExperiment experiment)
    {
        string result = "";
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
                result = "Minilab";
                break;
            default:
            case ExperimentType.GeoSample:
                result = "Deposit Sample";
                break;
        }

        return result + " Mission " + experiment.MissionNumber;
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
public class BiologyScienceExperiment : IScienceExperiment
{
    public float Progress { get; set; }
    public Vector3 TargetPosition;
    public int DurationDays { get; set; }
    public int Reward { get; set; }
    public int MissionNumber { get; set; }
    public ExperimentType Experiment { get { return ExperimentType.BioMinilab; } }

    public BiologyScienceExperiment() { }
    public BiologyScienceExperiment(int _missionNumber)
    {
        MissionNumber = _missionNumber;
        if (_missionNumber < 3)
        {
            Reward = 9000 + _missionNumber * 1000;
            DurationDays = _missionNumber + 1;
        }
        else if (_missionNumber < 7)
        {
            Reward = 9000 + _missionNumber * 2000;
            DurationDays = _missionNumber + 1;
        }
        else if (_missionNumber < 10)
        {
            Reward = 9000 + _missionNumber * 3500;
            DurationDays = _missionNumber + 2;
        }
        else
        {
            Reward = 9000 + _missionNumber * 5000;
            DurationDays = _missionNumber + 2;
        }
    }
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

    public GeologyScienceExperiment() { }

    public GeologyScienceExperiment(int _missionNumber)
    {
        MissionNumber = _missionNumber;
        if (_missionNumber < 4)
        {
            Reward = 5000 + _missionNumber * 500;
        }
        else if (_missionNumber < 7)
        {
            Reward = 5000 + _missionNumber * 1000;
        }
        else if (_missionNumber < 10)
        {
            Reward = 5000 + _missionNumber * 1500;
        }
        else
        {
            Reward = 5000 + _missionNumber * 2000;
        }
    }
}

public static class Science {
    private static List<BiologyScienceExperiment> BioExperiments = new List<BiologyScienceExperiment>()
    {
        new BiologyScienceExperiment(1),
        new BiologyScienceExperiment(2),
        new BiologyScienceExperiment(3),
        new BiologyScienceExperiment(4),
        new BiologyScienceExperiment(5),
        new BiologyScienceExperiment(6),
        new BiologyScienceExperiment(7),
        new BiologyScienceExperiment(8),
        new BiologyScienceExperiment(9),
        new BiologyScienceExperiment(10),
    };
    private static List<GeologyScienceExperiment> GeoExperiments = new List<GeologyScienceExperiment>()
    {
        new GeologyScienceExperiment(1),
        new GeologyScienceExperiment(2),
        new GeologyScienceExperiment(3),
        new GeologyScienceExperiment(4),
        new GeologyScienceExperiment(5),
        new GeologyScienceExperiment(6),
        new GeologyScienceExperiment(7),
        new GeologyScienceExperiment(8),
        new GeologyScienceExperiment(9),
        new GeologyScienceExperiment(10),
    };

    public static BiologyScienceExperiment[] GetAvailableBiologyExperiments()
    {
        if (Base.Current.CompletedBiologyMissions == null)
            return BioExperiments.ToArray();
        else
        {
            return BioExperiments.Where(x => !Base.Current.CompletedBiologyMissions.Contains(x.MissionNumber)).ToArray();
        }
    }

    public static GeologyScienceExperiment[] GetAvailableGeologyExperiments()
    {
        if (Base.Current.CompletedGeologyMissions == null)
            return GeoExperiments.ToArray();
        else
        {
            return GeoExperiments.Where(x => !Base.Current.CompletedGeologyMissions.Contains(x.MissionNumber)).ToArray();
        }
    }
}
