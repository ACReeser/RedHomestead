using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class Pipe : MonoBehaviour {
    public MeshFilter MeshFilter, NorthVis, SouthVis;
    public Mesh[] CompoundUVSet = new Mesh[7];
    public Mesh[] NorthFlowVisualizationUVSet = new Mesh[7];
    public Mesh[] SouthFlowVisualizationUVSet = new Mesh[7];
    private Compound _pipeType = Compound.Unspecified;
    internal Compound PipeType
    {
        get
        {
            return _pipeType;
        }
        set
        {
            SetPipeType(value);
        }
    }

    internal Transform from, to;

    private void SetPipeType(Compound value)
    {
        _pipeType = value;
        if (_pipeType != Compound.Unspecified)
        {
            int index = ((int)_pipeType) + 1;

            if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
            {
                MeshFilter.mesh = CompoundUVSet[index];
                NorthVis.mesh = NorthFlowVisualizationUVSet[index];
                SouthVis.mesh = SouthFlowVisualizationUVSet[index];
            }
        }
    }
}
