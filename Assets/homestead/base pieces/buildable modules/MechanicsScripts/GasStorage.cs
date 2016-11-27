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
        if(CompoundUVSet[(int)this.SinkType] != null)
        {
            this.MeshFilter.mesh = CompoundUVSet[(int)this.SinkType];
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
