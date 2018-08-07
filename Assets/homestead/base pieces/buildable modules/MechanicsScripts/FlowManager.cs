using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;
using System;

public delegate void FlowTickHandler();

public class FlowManager : MonoBehaviour
{
    private const float TickPeriodSeconds = 1f;
    public ElectricityIndicatorMeshes GeneratorMeshes;
    public ElectricityIndicatorMeshes BatteryMeshes;
    public ElectricityIndicatorMeshesForConsumers ConsumerMeshes;

    internal event FlowTickHandler OnFlowTick;

    public static FlowManager Instance { get; private set; }

    public PowerGrids PowerGrids = new PowerGrids();
    [HideInInspector]
    public List<SingleResourceModuleGameplay> Sinks = new List<SingleResourceModuleGameplay>();
    [HideInInspector]
    public List<Converter> Converters = new List<Converter>();
    [HideInInspector]
    public Dictionary<string, Deposit> DepositMap = new Dictionary<string, Deposit>();

    private System.Diagnostics.Stopwatch powerAndIndustryStopwatch = new System.Diagnostics.Stopwatch();
    void Awake() {
        Instance = this;
        StartCoroutine(PowerAndIndustryUpdate());
    }

    private IEnumerator PowerAndIndustryUpdate()
    {
        while (isActiveAndEnabled)
        {
            powerAndIndustryStopwatch.Reset();
            powerAndIndustryStopwatch.Start();
            PowerGrids.Tick();
            IndustryUpdate();

            if (OnFlowTick != null)
                OnFlowTick();
            powerAndIndustryStopwatch.Stop();

            yield return new WaitForSeconds(TickPeriodSeconds - (float)powerAndIndustryStopwatch.Elapsed.TotalSeconds);
        }
    }

    private void IndustryUpdate() {
	    foreach(Converter c in Converters)
        {
            if (c.isActiveAndEnabled)
                c.Tick();
        }

        foreach (SingleResourceModuleGameplay s in Sinks)
        {
            if (s.isActiveAndEnabled)
                s.Tick();
        }
    }
}
