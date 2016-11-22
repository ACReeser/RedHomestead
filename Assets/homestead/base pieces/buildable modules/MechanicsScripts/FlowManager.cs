using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    public List<ModuleGameplay> Nodes = new List<ModuleGameplay>();

    void Awake () {
        Instance = this;    
	}
	
	
	void FixedUpdate () {
	
	}
}
