using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Industry;

public class Pipe : MonoBehaviour, IDataContainer<PipelineData> {
    public MeshFilter MeshFilter, NorthVis, SouthVis;
    public Mesh[] CompoundUVSet = new Mesh[7];
    public Mesh[] NorthFlowVisualizationUVSet = new Mesh[7];
    public Mesh[] SouthFlowVisualizationUVSet = new Mesh[7];

    private PipelineData data;
    public PipelineData Data { get { return data; } set { data = value; } }

    internal void AssignConnections(Matter matterType, ModuleGameplay from, ModuleGameplay to, Transform fromT, Transform toT)
    {
        if (Data == null)
            Data = new PipelineData();

        Data.MatterType = matterType;
        Data.From = from;
        Data.To = to;

        if (fromT != null && toT != null)
        {
            Data.fromPos = fromT.position;
            Data.toPos = toT.position;
        }
        
        this.transform.GetChild(0).localScale = new Vector3(1f, 1f, (Vector3.Distance(data.fromPos, data.toPos) / 2) * 10f);

        if (from is GasStorage)
        {
            (from as GasStorage).SpecifyCompound(matterType);
        }
        else if (to is GasStorage)
        {
            (to as GasStorage).SpecifyCompound(matterType);
        }

        IndustryExtensions.AddAdjacentPumpable(from, to);

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
