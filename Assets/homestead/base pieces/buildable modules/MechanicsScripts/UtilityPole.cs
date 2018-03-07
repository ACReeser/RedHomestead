using RedHomestead.Buildings;
using RedHomestead.Electricity;
using RedHomestead.Persistence;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UtilityPole : ResourcelessGameplay, IPowerConsumer
{
    public Light[] lights = new Light[2];

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public bool IsOn { get; set; }

    public override Module GetModuleType()
    {
        return Module.UtilityPole;
    }

    protected override void OnStart()
    {
        SunOrbit.Instance.OnHourChange += _OnHourChange;
        RefreshLights();
        base.OnStart();
    }

    private void _OnHourChange(int sol, float hour)
    {
        bool hourIsSix = hour > 5f || hour < 7f;
        bool hourIsEighteen = hour > 17f && hour < 19f;
        if (hourIsSix || hourIsEighteen)
        {
            RefreshLights();
        }
    }

    private void RefreshLights()
    {
        float hour = Game.Current.Environment.CurrentHour;
        foreach (Light t in lights)
        {
            t.enabled = HasPower && (hour < 6f || hour > 17f);
        }
    }

    public override void OnPowerChanged()
    {
        RefreshLights();
        base.OnPowerChanged();
    }

    public override void OnAdjacentChanged()
    {
    }

    public override void Report()
    {
    }

    public override void Tick()
    {
    }

    public void OnEmergencyShutdown()
    {
        RefreshLights();
    }
}
