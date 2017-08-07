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

public class SolarPanel : ResourcelessGameplay, IVariablePowerSupply, IFlexDataContainer<ResourcelessModuleData, SolarPanelFlexData>
{
    public const float MinimumEfficiency = .14f;
    public const float MaximumEfficiency = .38f;
    public const float EquatorAnnualAverageInsolationPerMeter2 = 190f;
    public const float EquatorAnnualMaximumInsolationPerMeter2 = 210f;
    public const float SouthPoleAnnualMaximumInsolationPerMeter2 = 300f;
    public const float SouthPoleAnnualAverageInsolationPerMeter2 = 90f;
    public const int Meter2PerModule = 16;
    public const float MaximumWattsPerModule = EquatorAnnualAverageInsolationPerMeter2 * Meter2PerModule * MinimumEfficiency;

    internal static List<SolarPanel> AllPanels = new List<SolarPanel>();

    public SolarPanelFlexData FlexData { get; set; }

    public MeshFilter powerBacking { get; set; }
    public Transform powerMask { get; set; }
    public Transform[] PivotPoles = new Transform[2];
    public MeshRenderer panel1Mesh, panel2Mesh;
    //public BoxCollider panel1Collider, panel2Collider; 

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

    private const float TiltAmount = 50f;
    private void _OnHourChange(int sol, float hour)
    {
        //"preview" solar panels were throwing errors
        if (this.gameObject.activeInHierarchy && hour < 19 && hour > 5)
        {
            float tiltAmount = Mathf.Lerp(90f + TiltAmount, 90f - TiltAmount, (float)(hour - 6f) / 12f);
            StartCoroutine(RotateTo(tiltAmount));
        }
    }

    private IEnumerator RotateTo(float tiltAmount)
    {
        Quaternion from = PivotPoles[0].localRotation;
        Quaternion to = Quaternion.Euler(0f, -90f, tiltAmount);

        float time = 0f;
        while(time < 1f)
        {
            foreach(Transform panel in PivotPoles)
            {
                panel.localRotation = Quaternion.Lerp(from, to, time);
            }            
            time += Time.deltaTime;
            yield return null;
        }
        foreach (Transform panel in PivotPoles)
        {
            panel.localRotation = to;
        }
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

    public override Module GetModuleType()
    {
        return Module.SolarPanelSmall;
    }

    protected override void OnStart()
    {
        SunOrbit.Instance.OnHourChange += _OnHourChange;
        _OnHourChange(Game.Current.Environment.CurrentSol, Game.Current.Environment.CurrentHour);
        AllPanels.Add(this);

        if (SunOrbit.DustManager != null)
            SunOrbit.DustManager.OnSolarPanelAdded(this);
    }

    public void RefreshSolarPanelDustVisuals()
    {
        bool hasDust = FlexData.DustBuildup > 0;
        panel1Mesh.transform.gameObject.SetActive(hasDust);
        panel2Mesh.transform.gameObject.SetActive(hasDust);
        if (hasDust)
        {
            float dustCutoff = Mathf.Lerp(1f, .22f, FlexData.DustBuildup);
            panel1Mesh.material.SetFloat("_Cutoff", dustCutoff);
            panel2Mesh.material.SetFloat("_Cutoff", dustCutoff);
        }
    }
}
