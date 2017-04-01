using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;

public class Powerline : MonoBehaviour, IDataContainer<PowerlineData> {
    private PowerlineData data;
    public PowerlineData Data { get { return data; } set { data = value; } }

    internal void AssignConnections(ModuleGameplay from, ModuleGameplay to)
    {
        Data = new PowerlineData()
        {
            From = from,
            To = to
        };

        FlowManager.Instance.PowerGrids.Attach(from, to);
    }
}
