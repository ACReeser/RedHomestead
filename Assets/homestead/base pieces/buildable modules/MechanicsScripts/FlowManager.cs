using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    public List<PowerSupply> PowerSupplies = new List<PowerSupply>();
    public List<Sink> Sinks = new List<Sink>();
    public List<Converter> Converters = new List<Converter>();

    void Awake () {
        Instance = this;    
	}	
	
	void FixedUpdate () {
	    foreach(Converter c in Converters)
        {
            c.Tick();
        }

        foreach (Sink s in Sinks)
        {
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
