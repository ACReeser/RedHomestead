using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ExperimentType { GeoSample, BioMinilab }
public enum ExperimentStatus { Available, Accepted, ReadyForCompletion, Completed }

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
                result = "Sample";
                break;
        }

        return result + " Mission " + experiment.MissionNumber;
    }
}

public interface IScienceExperiment
{
    ExperimentType Experiment { get; }
    float Progress { get; }
    int Reward { get; }
    int DurationDays { get; }
    int MissionNumber { get; }
    void OnAccept();
    void OnCancel();
    void OnComplete();
    ExperimentStatus Status { get; }
    string ProgressText { get; }
}

public abstract class BaseScienceExperiment
{
    public abstract int MissionNumber { get; set; }
    public abstract ExperimentType Experiment { get; }

    public override bool Equals(object obj)
    {
        if (obj is BaseScienceExperiment)
        {
            var other = obj as BaseScienceExperiment;
            return this.MissionNumber == other.MissionNumber && this.Experiment == other.Experiment;
        }
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

[Serializable]
public class BiologyScienceExperiment : BaseScienceExperiment, IScienceExperiment
{
    public float Progress { get; set; }
    public Vector3 TargetPosition;
    public int DurationDays { get; set; }
    public int Reward { get; set; }
    public override int MissionNumber { get; set; }
    public override ExperimentType Experiment { get { return ExperimentType.BioMinilab; } }

    public BiologyScienceExperiment() { }
    public BiologyScienceExperiment(int _missionNumber)
    {
        Progress = -1f;
        MissionNumber = _missionNumber;
        if (_missionNumber < 3)
        {
            Reward = 9000 + _missionNumber * 1000;
            DurationDays = _missionNumber;
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

    public ExperimentStatus Status
    {
        get
        {
            if (Progress < 0)
            {
                return ExperimentStatus.Available;
            }
            else if (Progress >= DurationDays)
            {
                return ExperimentStatus.ReadyForCompletion;
            }
            else
            {
                return ExperimentStatus.Accepted;
            }
        }
    }

    public string ProgressText
    {
        get
        {
            if (Progress <= 0f)
            {
                return DurationDays + " DAY DURATION";
            }
            else
            {
                return "Day " + Progress + " of " + DurationDays;
            }
        }
    }

    public void OnAccept()
    {
        Progress = 0f;
    }

    public void OnComplete()
    {
    }

    public void OnCancel()
    {
    }
}

[Serializable]
public class GeologyScienceExperiment : BaseScienceExperiment, IScienceExperiment
{
    public string DepositID;
    public float Progress { get; set; }
    public int Reward { get; set; }
    public int DurationDays { get; set; }
    public override int MissionNumber { get; set; }
    public override ExperimentType Experiment { get { return ExperimentType.GeoSample; } }

    public GeologyScienceExperiment() { }

    public GeologyScienceExperiment(int _missionNumber)
    {
        Progress = -1f;
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
        DurationDays = 1;
    }

    public void OnAccept()
    {
        Progress = 0f;
        int randomI = UnityEngine.Random.Range(0, FlowManager.Instance.DepositMap.Keys.Count);
        string targetDepositID = FlowManager.Instance.DepositMap.Keys.ElementAt(randomI);
        this.DepositID = targetDepositID;
        Deposit target = FlowManager.Instance.DepositMap[targetDepositID];
        PlayerInput.Instance.ScienceExperimentMarkers[Convert.ToInt32(ExperimentType.GeoSample)].transform.position = target.transform.position;
        PlayerInput.Instance.ScienceExperimentMarkers[Convert.ToInt32(ExperimentType.GeoSample)].gameObject.SetActive(true);
    }

    public void OnComplete()
    {
        PlayerInput.Instance.ScienceExperimentMarkers[Convert.ToInt32(ExperimentType.GeoSample)].gameObject.SetActive(false);
    }

    public void OnCancel()
    {
        PlayerInput.Instance.ScienceExperimentMarkers[Convert.ToInt32(ExperimentType.GeoSample)].gameObject.SetActive(false);
    }

    public ExperimentStatus Status
    {
        get
        {
            if (Progress < 0)
            {
                return ExperimentStatus.Available;
            }
            else if (Progress >= 2f)
            {
                return ExperimentStatus.Completed;
            }
            else if (Progress >= DurationDays)
            {
                return ExperimentStatus.ReadyForCompletion;
            }
            else
            {
                return ExperimentStatus.Accepted;
            }
        }
    }

    public string ProgressText
    {
        get
        {
            if (Progress <= 0f)
            {
                if (DurationDays > 1)
                    return DurationDays + " SAMPLES";
                else
                    return DurationDays + " SAMPLE";
            }
            else if (Progress >= 2f)
            {
                return "";
            }
            else
            {
                return "SAMPLE " + Progress + " of " + DurationDays;
            }
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
            GeoExperiments.ForEach((geo) =>
            {
                if (Base.Current.CompletedGeologyMissions.Contains(geo.MissionNumber))
                {
                    geo.Progress = 2f;
                }
            });
            return GeoExperiments.ToArray(); //.Where(x => !Base.Current.CompletedGeologyMissions.Contains(x.MissionNumber)).ToArray();
        }
    }

    public static void Complete(this IScienceExperiment experiment)
    {
        switch (experiment.Experiment)
        {
            case ExperimentType.BioMinilab:
                Base.Current.CompletedBiologyMissions = getNewCompletedScienceMissionArray(Base.Current.CompletedBiologyMissions, experiment);
                break;
            case ExperimentType.GeoSample:
                Base.Current.CompletedGeologyMissions = getNewCompletedScienceMissionArray(Base.Current.CompletedGeologyMissions, experiment);
                break;
        }
        EconomyManager.Instance.ScienceExperimentPayday(experiment);
    }

    private static int[] getNewCompletedScienceMissionArray(int[] previouslyCompletedMissions, IScienceExperiment experiment)
    {
        if (previouslyCompletedMissions == null)
            return new int[1] { experiment.MissionNumber };
        else
        {
            var list = previouslyCompletedMissions.ToList();
            list.Add(experiment.MissionNumber);
            return list.ToArray();
        }
    }
}
