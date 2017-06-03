using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;
using System;

public class FlowManager : MonoBehaviour
{
    public ElectricityIndicatorMeshes GeneratorMeshes;
    public ElectricityIndicatorMeshes BatteryMeshes;
    public ElectricityIndicatorMeshesForConsumers ConsumerMeshes;

    public static FlowManager Instance { get; private set; }

    public PowerGrids PowerGrids = new PowerGrids();
    [HideInInspector]
    public List<SingleResourceModuleGameplay> Sinks = new List<SingleResourceModuleGameplay>();
    [HideInInspector]
    public List<Converter> Converters = new List<Converter>();

    void Awake() {
        Instance = this;
        StartCoroutine(PowerAndIndustryUpdate());
    }

    private IEnumerator PowerAndIndustryUpdate()
    {
        while (isActiveAndEnabled)
        {
            PowerGrids.Tick();
            IndustryUpdate();

            yield return new WaitForSeconds(1f);
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
