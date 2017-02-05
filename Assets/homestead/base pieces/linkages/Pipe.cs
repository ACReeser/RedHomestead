using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;

public class Pipe : MonoBehaviour {
    public MeshFilter MeshFilter, NorthVis, SouthVis;
    public Mesh[] CompoundUVSet = new Mesh[7];
    public Mesh[] NorthFlowVisualizationUVSet = new Mesh[7];
    public Mesh[] SouthFlowVisualizationUVSet = new Mesh[7];
    private Matter _pipeType = Matter.Unspecified;
    internal Matter PipeType
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

    private void SetPipeType(Matter value)
    {
        _pipeType = value;
        if (_pipeType != Matter.Unspecified)
        {
            int index = Math.Abs((int)_pipeType);

            if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
            {
                MeshFilter.mesh = CompoundUVSet[index];
                NorthVis.mesh = NorthFlowVisualizationUVSet[index];
                SouthVis.mesh = SouthFlowVisualizationUVSet[index];
            }
        }
    }
}
