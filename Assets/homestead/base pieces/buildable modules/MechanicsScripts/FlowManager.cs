using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

#warning todo: instead track powergrid/islands, each grid has list of consumers + producers
    public List<PowerSupply> PowerSupplies = new List<PowerSupply>();
    public List<SingleResourceModuleGameplay> Sinks = new List<SingleResourceModuleGameplay>();
    public List<Converter> Converters = new List<Converter>();

    void Awake() {
        Instance = this;    
	}	
	
	void FixedUpdate() {
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

    public void RecalculatePowerConnections()
    {

    }

    public void RecalculateAvailablePower()
    {

    }
}
