﻿using RedHomestead.Equipment;
using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MinilabData : FacingData
{
    public string ScienceLabID;
    public float HoursAlive;
    public float HoursRequired;

    /// <summary>
    /// normalized 0 to 1 progress
    /// </summary>
    public float Progress
    {
        get
        {
            return Mathf.Min(1f, HoursAlive / HoursRequired);
        }
    }
}

public class Minilab : MovableSnappable, IDataContainer<MinilabData>
{
    private MinilabData data;
    
    public MinilabData Data { get { return data; } set { data = value; } }

    private ScienceLab Dad;
    
    public void Start()
    {
        if (Data != null)
        {
            foreach(var lab in ScienceLab.ActiveLabs)
            {
                if (lab.Data.ModuleInstanceID == Data.ScienceLabID)
                {
                    lab.Adopt(this);
                    Dad = lab;
                }
            }
            if (Dad == null)
            {
                UnityEngine.Debug.LogWarning("No science lab dad found on load!");
            }
        }
        SunOrbit.Instance.OnHourChange += Instance_OnHourChange;
    }

    private void Instance_OnHourChange(int sol, float hour)
    {
        Data.HoursAlive++;
        if (Dad == null)
        {
            UnityEngine.Debug.LogWarning("No science lab dad to update experiment!");
        }
        else
        {
            Dad.FlexData.CurrentBioExperiment.Progress = this.Data.HoursAlive / SunOrbit.MartianHoursPerDay;
        }
        if (Data.Progress >= 1f)
        {
            GuiBridge.Instance.ShowNews(NewsSource.MinilabDone);
        }
    }

    public override string GetText()
    {
        return "Minilab";
    }

    public override float Progress
    {
        get { return Data.Progress; }
    }

    /// <summary>
    /// should only be called once, when an experiment is accepted
    /// </summary>
    /// <param name="dad"></param>
    internal void Assign(ScienceLab dad)
    {
        Dad = dad;

        if (Data == null)
        {
            Data = new MinilabData();
        }
        Data.ScienceLabID = dad.Data.ModuleInstanceID;
        Data.HoursRequired = dad.FlexData.CurrentBioExperiment.DurationDays * SunOrbit.MartianHoursPerDay;
    }
}
