using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Industry;
using System;

[Serializable]
public struct PipeStructure
{
    public Transform ElbowUp, Vertical, ElbowOver;
    public MeshFilter VerticalMeshFilter;
}

public class Pipeline : MonoBehaviour, IDataContainer<PipelineData>
{
    public MeshFilter MeshFilter, NorthVis, SouthVis;
    public Transform Crosspipe;
    public PipeStructure Alpha, Beta;
    public Mesh[] CompoundUVSet = new Mesh[7];
    public Mesh[] NorthFlowVisualizationUVSet = new Mesh[7];
    public Mesh[] SouthFlowVisualizationUVSet = new Mesh[7];

    private PipelineData data;
    public PipelineData Data { get { return data; } set { data = value; } }

    private const float MinElbowOverZ = 0.525f;
    private const float ElbowYRadius = 0.26f;
    private const float ElbowYDiameter = ElbowYRadius * 2f;
    private const float ElbowXZRadius = 0.2f;
    private const float ElbowXZDiameter = ElbowXZRadius * 2f;
    private const float VerticalLocalX = 0.4450269f;

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

        Transform lowerT = fromT, higherT = toT;
        if (fromT.position.y > toT.position.y)
        {
            lowerT = toT;
            higherT = fromT;
        }

        Alpha.ElbowUp.position = lowerT.position;
        Alpha.ElbowUp.rotation = lowerT.rotation * Quaternion.Euler(-90f, 0f, -90f);
        Beta.ElbowUp.position = higherT.position;
        Beta.ElbowUp.rotation = higherT.rotation * Quaternion.Euler(-90f, 0f, -90f);

        float valveYDistanceDelta = higherT.position.y - lowerT.position.y;
        //if (valveYDistanceDelta < MinElbowOverZ)
        //    valveYDistanceDelta = MinElbowOverZ;

        Alpha.ElbowOver.localPosition = new Vector3(0.448f, 0f, MinElbowOverZ + valveYDistanceDelta);
        //for now, beta (higher) is at the minimum
        //it may need to be at some other minimum, causing "arching" pipes
        Beta.ElbowOver.localPosition = new Vector3(0.448f, 0f, MinElbowOverZ);

        Vector3 alphaElbowLookAtBeta = new Vector3(Beta.ElbowOver.position.x, Alpha.ElbowOver.position.y, Beta.ElbowOver.position.z);
        Alpha.ElbowOver.LookAt(alphaElbowLookAtBeta);
        Alpha.ElbowOver.localRotation *= Quaternion.Euler(0f, 0f, -90f);
        Vector3 betaElbowLookAtBeta = new Vector3(Alpha.ElbowOver.position.x, Beta.ElbowOver.position.y, Alpha.ElbowOver.position.z);
        Beta.ElbowOver.LookAt(betaElbowLookAtBeta);
        Beta.ElbowOver.localRotation *= Quaternion.Euler(0f, 0f, -90f);

        Crosspipe.position = (Alpha.ElbowOver.position + Beta.ElbowOver.position) / 2f;
        Crosspipe.position = new Vector3(Crosspipe.position.x, Beta.ElbowOver.position.y, Crosspipe.position.z);
        Crosspipe.LookAt(Alpha.ElbowOver);

        Crosspipe.localScale = new Vector3(1f, 1f, ((Vector3.Distance(Alpha.ElbowOver.position, Beta.ElbowOver.position) - ElbowXZDiameter) / 2)  * 10f);
        
        Alpha.Vertical.localPosition = new Vector3(VerticalLocalX, 0f, Alpha.ElbowOver.localPosition.z / 2f);
        Alpha.Vertical.localScale = new Vector3(1f, 1f, ((Vector3.Distance(Alpha.ElbowUp.position, Alpha.ElbowOver.position) - ElbowYDiameter) / 2) * 10f);
        //for now we don't stretch the beta (higher) pipe
        //it may need to be at some other minimum, causing "arching" pipes
        //Beta.Vertical.localScale = new Vector3(1f, 1f, ((Vector3.Distance(Beta.ElbowUp.position, Beta.ElbowOver.position) / 2) - ElbowYDiameter) * 10f);

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
                Alpha.VerticalMeshFilter.mesh = CompoundUVSet[index];
                Beta.VerticalMeshFilter.mesh = CompoundUVSet[index];
                NorthVis.mesh = NorthFlowVisualizationUVSet[index];
                SouthVis.mesh = SouthFlowVisualizationUVSet[index];
            }
        }
    }
}

