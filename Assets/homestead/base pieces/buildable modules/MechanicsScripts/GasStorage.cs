using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;

public class GasStorage : SingleResourceSink {
    public MeshFilter MeshFilter;
    public Mesh[] CompoundUVSet = new Mesh[6];
    public Mesh UnspecifiedUV;
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

        if (this.SinkType != Matter.Unspecified)
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
        if (this.SinkType == Matter.Unspecified)
        {
            this.MeshFilter.mesh = UnspecifiedUV;
            flowAmountRenderer.color = CompoundColors[0];
        }
        else
        {
            int index = (int)this.SinkType + 6;
            if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
            {
                this.MeshFilter.mesh = CompoundUVSet[index];
            }
            flowAmountRenderer.color = CompoundColors[index];
        }
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

    public void SpecifyCompound(Matter c)
    {
        if (this.SinkType == Matter.Unspecified)
        {
            _SpecifyCompound(c);
        }
        else
        {
            print("cannot set this storage to compound type "+c.ToString());
        }
    }

    private void _SpecifyCompound(Matter c)
    {
        this.SinkType = c;
        SyncMeshToCompoundType();

        if (c == Matter.Unspecified)
        {
            this.Container = null;
        }
        else
        {
            this.Container = new ResourceContainer(StartAmount)
            {
                TotalCapacity = Capacity,
                MatterType = this.SinkType
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
            _SpecifyCompound(Matter.Unspecified);
        }
    }
}
