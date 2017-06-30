using UnityEngine;
using System.Collections;
using System;

public class PowerInterface : HabitatReadout
{

    public TextMesh DisplayOut;

    protected override void OnStart()
    {
        FlowManager.Instance.PowerGrids.OnPowerTick += PowerGrids_OnPowerTick;
        OnPowerChanged();
    }

    void OnDestroy()
    {
        FlowManager.Instance.PowerGrids.OnPowerTick -= PowerGrids_OnPowerTick;
    }

    private void PowerGrids_OnPowerTick()
    {
        OnPowerChanged();
    }

    private void OnPowerChanged()
    {
        DisplayOut.text = string.Format(@"Power: {0}

Rated: {1:0.#} W
Current: {2:0.#} W
Load: {3:0.#} W

Batteries: {4}

Installed: {5}
Charge: {6}
",
            FlowManager.Instance.PowerGrids.LastGlobalTickData.CapacityString,
            FlowManager.Instance.PowerGrids.LastGlobalTickData.RatedCapacityWatts,
            FlowManager.Instance.PowerGrids.LastGlobalTickData.CurrentCapacityWatts,
            FlowManager.Instance.PowerGrids.LastGlobalTickData.LoadWatts,
            FlowManager.Instance.PowerGrids.LastGlobalTickData.BatteryString,
            FlowManager.Instance.PowerGrids.LastGlobalTickData.InstalledBatteryWattHours,
            FlowManager.Instance.PowerGrids.LastGlobalTickData.CurrentBatteryWattHours
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
