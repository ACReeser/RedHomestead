using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using RedHomestead.Simulation;
using RedHomestead.Buildings;

public class GasStorage : SingleResourceModuleGameplay {
    public MeshFilter MeshFilter, CapMeshFilter;
    public Mesh[] CompoundUVSet = new Mesh[6];
    public Mesh[] CompoundCapUVSet = new Mesh[6];
    public Mesh UnspecifiedUV, CapUnspecifiedUV;
    public Color[] CompoundColors = new Color[6];

    public override bool CanMalfunction
    {
        get
        {
            if (Data.Container.CurrentAmount <= 0f)
                return false;

            return base.CanMalfunction;
        }
    }
    private bool isLeaking = false;
    private const float LeakPerTickUnits = 1 / 180f; //1 per 2 minutes

    public override float WattsConsumed
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
        
        SyncObjectsToCompound();
    }

    private void SyncObjectsToCompound()
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
                child.tag = PlayerInput.GetValveFromCompound(this.Data.Container.MatterType);
            }

            SetValveTagsToCompound(child);
        }
    }

    private void RefreshMeshToCompound()
    {
        if (this.ResourceType == Matter.Unspecified)
        {
            this.MeshFilter.mesh = UnspecifiedUV;
            this.CapMeshFilter.mesh = CapUnspecifiedUV;
            flowAmountRenderer.color = CompoundColors[0];
        }
        else
        {
            int index = (int)this.ResourceType + 6;
            if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
            {
                this.MeshFilter.mesh = CompoundUVSet[index];
                this.CapMeshFilter.mesh = CompoundCapUVSet[index];
            }
            flowAmountRenderer.color = CompoundColors[index];
        }
    }

    // Update is called once per frame
    void Update()
    {
        float percentage = 0f;
        if (this.Data.Container != null)
        {
            percentage = this.Data.Container.UtilizationPercentage;
        }

        flowAmountRenderer.transform.localScale = new Vector3(1, percentage, 1);
    }

    public void ToggleLeak(bool leaking)
    {
        this.isLeaking = leaking;
    }

    public void SpecifyCompound(Matter c)
    {
        if (this.ResourceType == Matter.Unspecified)
        {
            this.Data.Container.MatterType = c;
            SyncObjectsToCompound();
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

    public override void OnAdjacentChanged()
    {
        if (Adjacent.Count == 0 && this.Data.Container != null && this.Data.Container.UtilizationPercentage <= 0f)
        {
            this.Data.Container.MatterType = Matter.Unspecified;
            SyncObjectsToCompound();
        }
    }
    
    public override void Tick()
    {
        if (isLeaking)
        {
            this.Data.Container.Pull(LeakPerTickUnits);
        }
    }

    public override Module GetModuleType()
    {
        return Module.SmallGasTank;
    }

    public override ResourceContainer GetStartingDataContainer()
    {
        if (this.Data == null)
            return new ResourceContainer()
            {
                MatterType = Matter.Unspecified,
                TotalCapacity = 10f
            };
        else
            return this.Data.Container;
    }
}
