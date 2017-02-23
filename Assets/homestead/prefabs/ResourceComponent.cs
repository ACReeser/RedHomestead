using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;

public class ResourceComponent : MonoBehaviour {
    public Matter ResourceType;
    public float Quantity = 1;

    public Mesh[] ResourceLabelMeshes, CompoundLabelMeshes;
    public MeshFilter LabelMeshFilter;

    
    //todo: could be CurrentConstructionZone reference instead
    internal bool IsInConstructionZone = false;
    internal bool IsOutsideConstructionZone
    {
        get
        {
            return !IsInConstructionZone;
        }
    }


    void Start()
    {
        if (ResourceType != Matter.Unspecified)
        {
            int index = (int)ResourceType;

            if (index > 0)
                LabelMeshFilter.mesh = ResourceLabelMeshes[index - 1];
            else
                LabelMeshFilter.mesh = CompoundLabelMeshes[index + 6];
        }
    }
}
