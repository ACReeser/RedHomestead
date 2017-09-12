using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;
using RedHomestead.Persistence;

[Serializable]
public class CrateData : FacingData
{
    public ResourceContainer Container;
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
    public Material PrinterMaterial, RawMaterial;
    public MeshRenderer CrateRenderer;

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
        if (Data.Container.MatterType != Matter.Unspecified)
        {
            int index = (int)Data.Container.MatterType;

            if (index > 0)
                LabelMeshFilter.mesh = ResourceLabelMeshes[index];
            else
                LabelMeshFilter.mesh = CompoundLabelMeshes[index + 6];

            if (Data.Container.MatterType.Is3DPrinterFeedstock())
            {
                CrateRenderer.material = PrinterMaterial;
            }
            else if (Data.Container.MatterType.IsRawMaterial()) {
                CrateRenderer.material = RawMaterial;
            }
        }
    }

    public override string GetText()
    {
        return this.Data.Container.MatterType.ToString() + String.Format(" {0:0.##}kg", this.Data.Container.CurrentAmount * this.Data.Container.MatterType.Kilograms());
    }
}
