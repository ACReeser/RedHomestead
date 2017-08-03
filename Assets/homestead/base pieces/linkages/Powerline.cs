using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Electricity;

public class Powerline : MonoBehaviour, IDataContainer<PowerlineData> {
    private PowerlineData data;
    public Transform CapPrefab;
    public PowerlineData Data { get { return data; } set { data = value; } }
    private Transform[] Ends = new Transform[2];
    private bool IsCorridor = false;

    protected virtual Vector3 EndCapLocalPosition { get { return Vector3.zero; } }
    protected virtual Quaternion EndCapLocalRotation { get { return Quaternion.Euler(90f, 0f, -90f); } }
    protected virtual Vector3 EndCapWorldScale { get { return Vector3.one * .14f; } }

    internal void AssignConnections(IPowerable from, IPowerable to, Transform fromT, Transform toT)
    {
        AddOrAugmentData(from, to, fromT, toT);

        FlowManager.Instance.PowerGrids.Attach(this, from, to);

        IHabitatModule fromH = from as IHabitatModule;
        IHabitatModule toH = to as IHabitatModule;

        if (fromH != null && toH != null)
        {
            HabitatModuleExtensions.AssignConnections(fromH, toH);
            IsCorridor = true;
        }

        ShowVisuals(from, to);
    }

    private void AddOrAugmentData(IPowerable from, IPowerable to, Transform fromT, Transform toT)
    {
        if (data == null)
        {
            Data = new PowerlineData();
        }

        Data.From = from;
        Data.To = to;

        if (fromT != null && toT != null)
        {
            Data.fromPos = fromT.position + fromT.TransformVector(EndCapLocalPosition);
            Data.fromRot = fromT.rotation * EndCapLocalRotation;
            Data.fromScale = EndCapWorldScale;

            Data.toPos = toT.position + toT.TransformVector(EndCapLocalPosition);
            Data.toRot = toT.rotation * EndCapLocalRotation;
            Data.toScale = EndCapWorldScale;
        }
    }

    protected virtual void ShowVisuals(IPowerable from, IPowerable to)
    {
        if (CapPrefab != null)
        {
            Ends[0] = CreateCap(data.fromPos, data.fromRot, data.fromScale);
            Ends[1] = CreateCap(data.toPos, data.toRot, data.toScale);
        }

        this.transform.GetChild(0).localScale = new Vector3(1f, 1f, (Vector3.Distance(data.fromPos, data.toPos) / 2) * 10f);        
    }

    protected virtual void HideVisuals()
    {
        SetEnds(false);
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
    }

    internal void Remove()
    {
        if (Data == null || Data.From == null || Data.To == null)
        {
            UnityEngine.Debug.LogWarning("Powerline in removal mode not fully connected!");
        }
        else
        {
            HideVisuals();
            
            FlowManager.Instance.PowerGrids.Detach(this, Data.From, Data.To);

            GameObject.Destroy(gameObject);
        }
    }

    protected Transform CreateCap(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        var cap = GameObject.Instantiate<Transform>(CapPrefab);
        cap.SetParent(this.transform);
        cap.position = pos;
        cap.rotation = rot;
        cap.localScale = new Vector3(scale.x, scale.y, scale.z / this.transform.localScale.z);
        return cap;
    }
}
