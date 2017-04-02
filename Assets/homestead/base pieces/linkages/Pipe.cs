using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;

public class Pipe : MonoBehaviour, IDataContainer<PipelineData> {
    public MeshFilter MeshFilter, NorthVis, SouthVis;
    public Mesh[] CompoundUVSet = new Mesh[7];
    public Mesh[] NorthFlowVisualizationUVSet = new Mesh[7];
    public Mesh[] SouthFlowVisualizationUVSet = new Mesh[7];

    private PipelineData data;
    public PipelineData Data { get { return data; } set { data = value; } }

    internal void AssignConnections(Matter matterType, ModuleGameplay from, ModuleGameplay to)
    {
        Data = new PipelineData()
        {
            MatterType = matterType,
            From = from,
            To = to
        };

        if (from is GasStorage)
        {
            (from as GasStorage).SpecifyCompound(matterType);
        }
        else if (to is GasStorage)
        {
            (to as GasStorage).SpecifyCompound(matterType);
        }

        from.LinkToModule(to);
        to.LinkToModule(from);

        if (data.MatterType != Matter.Unspecified)
        {
            int index = (int)data.MatterType + 6;

            if (index < CompoundUVSet.Length && CompoundUVSet[index] != null)
            {
                MeshFilter.mesh = CompoundUVSet[index];
                NorthVis.mesh = NorthFlowVisualizationUVSet[index];
                SouthVis.mesh = SouthFlowVisualizationUVSet[index];
            }
        }
    }
}
