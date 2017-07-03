using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using System.Collections.Generic;

public interface IToggleReceiver
{
    void Toggle(Transform toggleHandle);
}

public static class ToggleMap
{
    public static Dictionary<Transform, IToggleReceiver> ToggleLookup = new Dictionary<Transform, IToggleReceiver>();
}

public class OxygenHeatInterface : HabitatResourceInterface, IToggleReceiver
{
    public SpriteRenderer flowAmountRenderer, oxygenSprite, heatSprite;
    public Color onColor, offColor, invalidColor;
    public Transform OxygenSwitch, HeatSwitch;


    // Use this for initialization
    protected override void OnStart()
    {
        base.OnStart();
        if (LinkedHab != null)
        {
            this.LinkedHab.OnPowerChange += this.OnPowerChange;
            this.OnPowerChange();
            this.RefreshSwitch(OxygenSwitch, this.LinkedHab.IsOxygenOn);
            this.RefreshSwitch(HeatSwitch, this.LinkedHab.IsHeatOn);
        }

        ToggleMap.ToggleLookup[this.OxygenSwitch] = this;
        ToggleMap.ToggleLookup[this.HeatSwitch] = this;
    }

    protected override void _OnDestroy()
    {
        base._OnDestroy();
        if (LinkedHab != null)
        {
            this.LinkedHab.OnPowerChange -= this.OnPowerChange;
        }
    }

    private void OnPowerChange()
    {
        oxygenSprite.color = !LinkedHab.HasPower ? this.invalidColor : LinkedHab.IsOxygenOn ? this.onColor : this.offColor;
        heatSprite.color = !LinkedHab.HasPower ? this.invalidColor : LinkedHab.IsHeatOn ? this.onColor : this.offColor;
    }

    protected override void OnResourceChange(params Matter[] changedMatter)
    {
        base.OnResourceChange();

        ResourceContainer container = LinkedHab.Get(DisplayResource);
        float percentage = 0f;
        if (container != null)
        {
            percentage = container.UtilizationPercentage;
        }

        flowAmountRenderer.transform.localScale = new Vector3(1, percentage, 1);
    }

    public void Toggle(Transform @switch)
    {
        if (this.LinkedHab)
        {
            if (@switch == this.OxygenSwitch)
            {
                this.LinkedHab.IsOxygenOn = !this.LinkedHab.IsOxygenOn;
                this.RefreshSwitch(OxygenSwitch, this.LinkedHab.IsOxygenOn);
            }
            else if (@switch == this.HeatSwitch)
            {
                this.LinkedHab.IsHeatOn = !this.LinkedHab.IsHeatOn;
                this.RefreshSwitch(HeatSwitch, this.LinkedHab.IsHeatOn);
            }
        }
    }

    static Quaternion offLocalRotation = Quaternion.Euler(-90f, 0f, 0f);
    private void RefreshSwitch(Transform @switch, bool isOn)
    {
        @switch.localRotation = isOn ? Quaternion.identity : offLocalRotation;
    }

    //private void DisplaySingleContainer()
    //{
    //    ResourceContainer container = LinkedHab.Get(DisplayResource);

    //    DisplayOut.text = string.Format("{0}: {1}\n{2}/{3}kg",
    //        HeaderText,
    //        container.UtilizationPercentageString(),
    //        container.CurrentAmount,
    //        container.TotalCapacity
    //        //container.LastTickRateOfChange == 0 ? " " : container.LastTickRateOfChange > 0 ? "+" : "-",
    //        //container.LastTickRateOfChange,
    //        //"g/s" //todo: make human readable with large/small quants and scales
    //        );
    //}
}
