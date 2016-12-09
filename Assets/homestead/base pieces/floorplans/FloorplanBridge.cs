using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Top level groups that organize floorplans
/// </summary>
public enum FloorplanGroup { Undecided = -1, Floor, Edge, Corner }

/// <summary>
/// Second level groups that organize floorplans
/// </summary>
public enum FloorplanSubGroup { Solid, Mesh, Door, Window, SingleColumn, DoubleColumn }

public enum FloorplanMaterial { Concrete, Brick, Metal, Plastic, Rock, Glass }

public class FloorplanBridge : MonoBehaviour {
    public static FloorplanBridge Instance;

    public Transform FloorPrefab, MeshFloorPrefab, WallPrefab, SingleCornerColumnPrefab, EdgeColumnPrefab, DoorPrefab;
    public Material ConcreteMaterial;
    
	// Use this for initialization
	void Awake () {
        Instance = this;
	}

    public Transform GetPrefab(out Material matchingMaterial)
    {
        return GetPrefab(GuiBridge.Instance.selectedFloorplanGroup, GuiBridge.Instance.selectedFloorplanSubgroup, GuiBridge.Instance.selectedFloorplanMaterial, out matchingMaterial);
    }

    public Transform GetPrefab(FloorplanGroup g, FloorplanSubGroup s, FloorplanMaterial mat, out Material matchingMaterial)
    {
        matchingMaterial = ConcreteMaterial;

        switch (g)
        {
            case FloorplanGroup.Floor:
                if (s == FloorplanSubGroup.Mesh)
                    return MeshFloorPrefab;
                else
                    return FloorPrefab;
            case FloorplanGroup.Edge:
                return GetEdgePrefab(s);
            case FloorplanGroup.Corner:
                return GetCornerPrefab(s);
        }

        return null;
    }

    private Transform GetCornerPrefab(FloorplanSubGroup s)
    {
        switch (s)
        {
            default:
                return SingleCornerColumnPrefab;
        }
    }

    private Transform GetEdgePrefab(FloorplanSubGroup s)
    {
        switch (s)
        {
            case FloorplanSubGroup.Door:
                return DoorPrefab;
            case FloorplanSubGroup.SingleColumn:
                return EdgeColumnPrefab;
            default:
                return WallPrefab;
        }
    }
}
