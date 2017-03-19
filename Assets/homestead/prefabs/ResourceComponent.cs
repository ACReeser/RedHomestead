using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;
using RedHomestead.Persistence;

[Serializable]
public class CrateData : FacingData
{
    public Matter ResourceType;
    public float Quantity = 1;
}

[RequireComponent(typeof(Rigidbody))]
public class ResourceComponent : MovableSnappable, IDataContainer<CrateData> {
    public CrateData data;
    public CrateData Data {
        get { return data;  }
        set { data = value; }
    }

    public Mesh[] ResourceLabelMeshes, CompoundLabelMeshes;
    public MeshFilter LabelMeshFilter;

    private Collider myCollider;
    //todo: could be CurrentConstructionZone reference instead
    internal bool IsInConstructionZone = false;
    internal bool IsOutsideConstructionZone
    {
        get
        {
            return !IsInConstructionZone;
        }
    }


    void Start()
    {
        myCollider = GetComponent<Collider>();
        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (Data.ResourceType != Matter.Unspecified)
        {
            int index = (int)Data.ResourceType;

            if (index > 0)
                LabelMeshFilter.mesh = ResourceLabelMeshes[index - 1];
            else
                LabelMeshFilter.mesh = CompoundLabelMeshes[index + 6];
        }
    }

    public override string GetText()
    {
        return this.Data.ResourceType.ToString() + String.Format(" {0:0.##}kg", this.Data.Quantity * this.Data.ResourceType.Kilograms());
    }
}
