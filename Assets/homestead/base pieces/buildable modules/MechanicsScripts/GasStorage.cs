using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;

public class GasStorage : SingleResourceSink {
    public MeshFilter MeshFilter;
    public Mesh[] CompoundUVSet = new Mesh[6];
    public Color[] CompoundColors = new Color[6];

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

        if (this.SinkType != Compound.Unspecified)
            _SpecifyCompound(this.SinkType);
        else
            SyncMeshToCompoundType();
    }

    private void SyncMeshToCompoundType()
    {
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
        flowAmountRenderer.color = CompoundColors[index];
    }

    // Update is called once per frame
    void Update()
    {
        float percentage = 0f;
        if (Container != null)
        {
            percentage = Container.UtilizationPercentage;
        }

        flowAmountRenderer.transform.localScale = new Vector3(1, percentage, 1);
    }

    public void SpecifyCompound(Compound c)
    {
        if (this.SinkType == Compound.Unspecified)
        {
            _SpecifyCompound(c);
        }
        else
        {
            print("cannot set this storage to compound type "+c.ToString());
        }
    }

    private void _SpecifyCompound(Compound c)
    {
        this.SinkType = c;
        SyncMeshToCompoundType();

        if (c == Compound.Unspecified)
        {
            this.Container = null;
        }
        else
        {
            this.Container = new ResourceContainer(StartAmount)
            {
                TotalCapacity = Capacity,
                SimpleCompoundType = this.SinkType
            };
        }
    }

    public override void Report()
    {
        throw new NotImplementedException();
    }

    public override void OnAdjacentChanged()
    {
        base.OnAdjacentChanged();

        if (Adjacent.Count == 0 && Container != null && Container.UtilizationPercentage <= 0f)
        {
            _SpecifyCompound(Compound.Unspecified);
        }
    }
}
