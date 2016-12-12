using UnityEngine;
using System.Collections;
using System;

public class PowerInterface : HabitatModule
{

    public TextMesh DisplayOut;

    protected override void OnStart()
    {
        OnPowerChanged();
    }

    void FixedUpdate()
    {
        OnPowerChanged();
    }

    private void OnPowerChanged()
    {
        float capacity = 100;
        float charge = 100;
        float generation = 10;
        float installedGeneration = 10;
        float pull = 10;

        DisplayOut.text = string.Format("Power\n\nGeneration: {9}%\nGenerated: {5}{6}\nInstalled: {7}{8}\n\nStorage: {4}%\nCharge: {0}{1}\nCapacity: {2}{3}\n\nDraw: {10}{11}\n",
            Math.Truncate(capacity),
            GetReadableMetricPower(capacity),
            Math.Truncate(charge),
            GetReadableMetricPower(charge),
            capacity <= 0 ? 0: Math.Truncate(charge/capacity*100),
            Math.Truncate(generation),
            GetReadableMetricPower(generation),
            Math.Truncate(installedGeneration),
            GetReadableMetricPower(installedGeneration),
            installedGeneration <= 0 ? 0 : Math.Truncate(generation / installedGeneration * 100),
            pull,
            GetReadableMetricPower(pull)+"/s"
            );
    }

    private static string GetReadableMetricPower(float watts)
    {
        return watts > 1000000 ? "MW" : watts > 1000 ? "kW" : "W";
    }

    private static string GetReadableMetricPowerHours(float wattHours)
    {
        return GetReadableMetricPower(wattHours) + "h";
    }
}
