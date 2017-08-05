using System;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using UnityEngine;
using RedHomestead.Electricity;

public class WeatherStation : ResourcelessGameplay, IPowerConsumer
{
    public static List<WeatherStation> AllWeatherStations = new List<WeatherStation>();

    public bool IsOn { get { return HasPower; } set { } }

    public override float WattsConsumed
    {
        get
        {
            return RedHomestead.Electricity.ElectricityConstants.WattsPerBlock;
        }
    }

    public override Module GetModuleType()
    {
        return Module.WeatherStation;
    }

    public override void OnAdjacentChanged()
    {
    }

    public void OnEmergencyShutdown()
    {
    }

    public override void Report()
    {
    }

    public override void Tick()
    {
    }

    protected override void OnStart () {
        AllWeatherStations.Add(this);
	}

    void OnDestroy()
    {
        AllWeatherStations.Remove(this);
    }
}
