using System.Collections;
using System.Collections.Generic;
using RedHomestead.Electricity;
using UnityEngine;
using RedHomestead.Persistence;
using System;
using RedHomestead.Buildings;

[Serializable]
public class CorridorFlexData
{
    public int FromBulkheadIndex, ToBulkheadIndex;
}

public class Corridor : Powerline, IFlexDataContainer<PowerlineData, CorridorFlexData>
{
    public CorridorFlexData FlexData { get; set; }

    protected override Vector3 EndCapLocalPosition { get { return Vector3.zero; } }
    protected override Quaternion EndCapLocalRotation { get { return Quaternion.identity; } }
    protected override Vector3 EndCapWorldScale { get { return Vector3.one; } }

    protected override void ShowVisuals(IPowerable from, IPowerable to)
    {
        Transform newCorridor = this.transform.GetChild(0);
        MeshFilter newCorridorFilter = newCorridor.GetComponent<MeshFilter>();

        //create the interior mesh
        Mesh baseCorridorMesh = newCorridorFilter.sharedMesh;
        Mesh newCorridorMesh = (Mesh)Instantiate(baseCorridorMesh);

        Transform anchorT1 = Ends[0];
        Mesh anchorM1 = anchorT1.GetComponent<MeshFilter>().mesh;

        Transform anchorT2 = Ends[1];
        Mesh anchorM2 = anchorT2.GetComponent<MeshFilter>().mesh;

        //modify the ends of the mesh
        Construction.SetCorridorVertices(newCorridor, newCorridorMesh, anchorT1, anchorM1, anchorT2, anchorM2);

        //assign the programmatically created mesh to the mesh filter
        newCorridorFilter.mesh = newCorridorMesh;
        //and tell the mesh collider to use this new mesh as well
        newCorridor.GetComponent<MeshCollider>().sharedMesh = newCorridorMesh;

        SetEndsActive(false);
    }

    private void SetEndsActive(bool state)
    {
        if (Ends[0] == null || Ends[1] == null)
        {
            UnityEngine.Debug.LogWarning("tried to show visuals but ends array has some nulls!");
        }
        else
        {
            Ends[0].gameObject.SetActive(state);
            Ends[1].gameObject.SetActive(state);
        }
    }

    protected override void OnAssign(IPowerable from, IPowerable to, Transform fromT, Transform toT)
    {
        this.Data.Type = PowerlineType.Corridor;

        IHabitatModule fromH = from as IHabitatModule;
        IHabitatModule toH = to as IHabitatModule;

        if (fromH != null && toH != null)
        {
            HabitatModuleExtensions.AssignConnections(fromH, toH);
            
            if (this.FlexData == null) //new corridor
            {
                if (fromT == null || toT == null)
                {
                    UnityEngine.Debug.LogWarning("Assigning corridor connections does not have flex data or transforms!");
                }
                else
                {
                    this.FlexData = new CorridorFlexData()
                    {
                        FromBulkheadIndex = fromH.GetBulkheadIndex(fromT),
                        ToBulkheadIndex = toH.GetBulkheadIndex(toT)
                    };

                    Ends[0] = fromT;
                    Ends[1] = toT;
                }
            }
            else //loading corridor from file
            {
                Ends[0] = fromH.Bulkheads[this.FlexData.FromBulkheadIndex];
                Ends[1] = toH.Bulkheads[this.FlexData.ToBulkheadIndex];
            }
        }
    }

    protected override void HideVisuals()
    {
        SetEndsActive(true);
    }
}
