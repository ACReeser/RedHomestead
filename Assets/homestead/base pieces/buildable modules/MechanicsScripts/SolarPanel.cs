using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Electricity;
using RedHomestead.Persistence;

public class SolarPanel : ResourcelessGameplay, IPowerSupply
{
    public float Efficiency = .22f;
    public float GrossSolarWattagePerTick = 20f;
    public MeshFilter powerBacking { get; set; }
    public Transform powerMask { get; set; }
    public Transform[] PivotPoles = new Transform[2];

    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public bool VariablePowerSupply { get { return true; } }
    public float WattsGenerated
    {
        get
        {
            return GrossSolarWattagePerTick * Efficiency;
        }
    }

    public override void OnAdjacentChanged()
    {
    }

    private const float TiltAmount = 50f;
    private void _OnHourChange(int sol, float hour)
    {
        if (hour < 19 && hour > 5)
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
    }

    public override Module GetModuleType()
    {
        return Module.SolarPanelSmall;
    }

    protected override void OnStart()
    {
        SunOrbit.Instance.OnHourChange += _OnHourChange;
        _OnHourChange(Game.Current.Environment.CurrentSol, Game.Current.Environment.CurrentHour);
    }
}
