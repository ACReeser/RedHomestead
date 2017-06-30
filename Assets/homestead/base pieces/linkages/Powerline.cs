using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Electricity;

public class Powerline : MonoBehaviour, IDataContainer<PowerlineData> {
    private PowerlineData data;
    public PowerlineData Data { get { return data; } set { data = value; } }
    private Transform[] Ends = new Transform[2];
    private bool IsCorridor = false;

    internal void AssignConnections(IPowerable from, IPowerable to, Transform fromT, Transform toT)
    {
        Data = new PowerlineData()
        {
            From = from,
            To = to
        };

        FlowManager.Instance.PowerGrids.Attach(this, from, to);

        IHabitatModule fromH = from as IHabitatModule;
        IHabitatModule toH = to as IHabitatModule;

        if (fromH != null && toH != null)
        {
            HabitatModuleExtensions.AssignConnections(fromH, toH);
            IsCorridor = true;
        }

        if (fromT != null)
        {
            Ends[0] = fromT.GetChild(0);
            Ends[1] = toT.GetChild(0);
            SetEnds(true);
        }
    }

    private void SetEnds(bool connected)
    {
        if (Ends[0] == null)
        {
            UnityEngine.Debug.LogWarning("Can't set active state of powerline end that is null!");
        }
        else if (this.IsCorridor)
        {
            Ends[0].gameObject.SetActive(!connected);
            Ends[1].gameObject.SetActive(!connected);
        }
        else //regular powerline
        {
            Ends[0].GetComponent<MeshRenderer>().enabled = connected;
            Ends[1].GetComponent<MeshRenderer>().enabled = connected;
        }
    }

    internal void Remove()
    {
        if (Data == null || Data.From == null || Data.To == null)
        {
            UnityEngine.Debug.LogWarning("Powerline in removal mode not fully connected!");

        }
        else
        {
            SetEnds(false);
            
            FlowManager.Instance.PowerGrids.Detach(this, Data.From, Data.To);

            GameObject.Destroy(gameObject);
        }
    }
}
