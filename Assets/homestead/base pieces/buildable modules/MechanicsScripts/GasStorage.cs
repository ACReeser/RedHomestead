using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class GasStorage : ModuleGameplay {
    public MeshFilter MeshFilter;
    public Mesh[] CompoundUVSet = new Mesh[6];
    public List<ModuleGameplay> Inputs = new List<ModuleGameplay>();
    public List<ModuleGameplay> Outputs = new List<ModuleGameplay>();

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void Tick()
    {
        foreach(Compound c in Containers.Keys)
        {
            foreach(ModuleGameplay o in Outputs)
            {
                if (o.Containers[c] != null)
                {
                    

                }
            }
        }
    }
}
