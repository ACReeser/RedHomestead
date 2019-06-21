using RedHomestead.Electricity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public interface IModal<T>
{
    bool IsOpen { get; }
    void Toggle();
    void Toggle(bool state);
    void Render(T obj);
}

public abstract class Modal<T> : IModal<T>
{
    public RectTransform Root;

    public bool IsOpen { get { return Root.gameObject.activeSelf; } }
    public void Toggle()
    {
        this.Toggle(!this.IsOpen);
    }
    public void Toggle(bool state)
    {
        Root.gameObject.SetActive(state);
    }

    public abstract void Render(T obj);
}

[Serializable]
public class PowerGridScreen: Modal<PowerGrid>
{
    public Text Status, Summary, Stats;

    public override void Render(PowerGrid grid)
    {
        Status.text = grid.Mode.DisplayText();
        Summary.text = grid.BatteryPowerDuration() + " hr Battery Power\n" + Mathf.FloorToInt(grid.Data.SurplusWatts / 1000) + "kW Surplus";
        Stats.text = "+" + grid.Data.RatedCapacityWatts + "kW -" + grid.Data.LoadWatts + "kW / " + grid.Data.CurrentBatteryWattHours + "kWh";
    }
}