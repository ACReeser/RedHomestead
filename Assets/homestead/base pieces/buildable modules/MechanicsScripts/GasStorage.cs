using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;

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
    protected override void OnStart()
    {
        base.OnStart();
        SyncMeshToCompoundType();
    }

    private void SyncMeshToCompoundType()
    {
        print(this.SinkType);
        RefreshMeshToCompound();
        SetValveTagsToCompound(this.transform);
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
        //unspecified == -1, so add 1 to get 0 based array index of meshes
        int index = ((int)this.SinkType) + 1;
        if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
        {
            this.MeshFilter.mesh = CompoundUVSet[index];
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SpecifyCompound(Compound c)
    {
        if (this.SinkType == Compound.Unspecified)
        {
            this.SinkType = c;
            SyncMeshToCompoundType();
            this.Container = new ResourceContainer(StartAmount)
            {
                TotalCapacity = Capacity,
                SimpleCompoundType = this.SinkType
            };
        }
        else
        {
            print("cannot set this storage to compound type "+c.ToString());
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }
}
