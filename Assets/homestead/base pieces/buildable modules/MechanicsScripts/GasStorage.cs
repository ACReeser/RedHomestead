using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class GasStorage : SingleResourceSink {
    public MeshFilter MeshFilter;
    public Mesh[] CompoundUVSet = new Mesh[6];

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0;
        }
    }

    // Use this for initialization
    void Start()
    {
        SyncMeshToCompoundType();
    }

    private void SyncMeshToCompoundType()
    {
        if (SinkType != Compound.Unspecified)
        {
            RefreshMeshToCompound();
            SetValveTagsToCompound(this.transform);
        }
    }

    //todo: bug
    //assumes that all interaction will be valves
    private void SetValveTagsToCompound(Transform t)
    {
        foreach(Transform child in t){
            //8 == interaction
            if (child.gameObject.layer == 8)
            {
                child.tag = PlayerInput.GetValveFromCompound(this.SinkType);
            }

            SetValveTagsToCompound(child);
        }
    }

    private void RefreshMeshToCompound()
    {
        if (CompoundUVSet[(int)this.SinkType] != null)
        {
            this.MeshFilter.mesh = CompoundUVSet[(int)this.SinkType];
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void SpecifyCompound(Compound c)
    {
        if (this.SinkType == Compound.Unspecified)
        {
            this.SinkType = c;
            SyncMeshToCompoundType();
        }
        else
        {
            throw new InvalidOperationException("cannot set compound to this type");
        }
    }
}
