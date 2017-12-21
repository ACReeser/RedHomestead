using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Electricity;
using RedHomestead.Persistence;
using System.Collections.Generic;

[Serializable]
public class SolarPanelFlexData
{
    public float DustBuildup;
}

public abstract class SolarPanel : ResourcelessGameplay, IVariablePowerSupply, IFlexDataContainer<ResourcelessModuleData, SolarPanelFlexData>
{
    public const float MinimumEfficiency = .14f;
    public const float HeliotropicEfficiency = .24f;
    public const float MaximumEfficiency = .38f;
    public const float EquatorAnnualAverageInsolationPerMeter2 = 190f;
    public const float EquatorAnnualMaximumInsolationPerMeter2 = 210f;
    public const float SouthPoleAnnualMaximumInsolationPerMeter2 = 300f;
    public const float SouthPoleAnnualAverageInsolationPerMeter2 = 90f;
    public const int Meter2PerModule = 16;
    public const float MaximumWattsPerModule = EquatorAnnualAverageInsolationPerMeter2 * Meter2PerModule * HeliotropicEfficiency;

    internal static List<SolarPanel> AllPanels = new List<SolarPanel>();

    public SolarPanelFlexData FlexData { get; set; }

    public MeshFilter powerBacking { get; set; }
    public Transform powerMask { get; set; }
    public MeshRenderer panelMesh;

    internal float Efficiency = MinimumEfficiency;

    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public float MaximumWattsGenerated { get { return EquatorAnnualAverageInsolationPerMeter2 * Meter2PerModule * Efficiency; } }

    public float WattsGenerated
    {
        get
        {
            return GetWattsAtHour();
        }
    }

    private float GetWattsAtHour()
    {
        return MaximumWattsGenerated * Game.Current.Environment.SolarIntensity() * (1f - this.FlexData.DustBuildup);
    }

    public override void OnAdjacentChanged()
    {
    }

    public override void Tick()
    {
        
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }

    public override void InitializeStartingData()
    {
        this.Data = new ResourcelessModuleData()
        {
            ModuleInstanceID = Guid.NewGuid().ToString(),
            ModuleType = GetModuleType()
        };
        this.FlexData = new SolarPanelFlexData();
    }

    protected override void OnStart()
    {
        AllPanels.Add(this);

        if (SunOrbit.DustManager != null)
            SunOrbit.DustManager.OnSolarPanelAdded(this);
    }

    public void RefreshSolarPanelDustVisuals()
    {
        bool hasDust = FlexData.DustBuildup > 0;
        panelMesh.transform.gameObject.SetActive(hasDust);
        if (hasDust)
        {
            float dustCutoff = Mathf.Lerp(1f, .22f, FlexData.DustBuildup);
            panelMesh.material.SetFloat("_Cutoff", dustCutoff);
        }
    }

    public void OnDestroy()
    {
        AllPanels.Remove(this);
    }
}
