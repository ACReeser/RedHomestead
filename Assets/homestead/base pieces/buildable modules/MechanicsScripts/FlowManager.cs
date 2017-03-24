using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    public PowerGrids PowerGrids = new PowerGrids();

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

        PowerGrids.Tick();
    }
}
