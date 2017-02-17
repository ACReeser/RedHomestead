using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Interiors;
using UnityEngine.UI;

[Serializable]
public struct FloorplanPrefabs
{
    public Transform[] SubGroupMeshes;
    public Material[] Materials;
}

[Serializable]
public struct StuffFields
{
    public Transform[] Prefabs;
    public Sprite[] Sprites;
    public RectTransform StuffPanel, StuffGroupsPanel, StuffGroupDetailPanel, StuffButtonsParent;
}

public class FloorplanBridge : MonoBehaviour {
    public static FloorplanBridge Instance;
    
    public Transform FloorPrefab, MeshFloorPrefab, WallPrefab, SingleCornerColumnPrefab, EdgeColumnPrefab, DoorPrefab;
    public Material ConcreteMaterial;
    public FloorplanPrefabs Floorplans;
    public StuffFields StuffFields;

    internal Stuff CurrentStuffBuild;

	// Use this for initialization
	void Awake () {
        Instance = this;
        ToggleStuffPanel(false);
    }

    internal void ToggleStuffPanel(bool state)
    {
        this.StuffFields.StuffPanel.gameObject.SetActive(state);
        this.StuffFields.StuffGroupsPanel.gameObject.SetActive(state);
        this.StuffFields.StuffGroupDetailPanel.gameObject.SetActive(false);

        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }

    public void SelectStuffGroup(int index)
    {
        this.StuffFields.StuffGroupsPanel.gameObject.SetActive(false);
        FillStuffDetail((StuffGroup)index);
        this.StuffFields.StuffGroupDetailPanel.gameObject.SetActive(true);
    }

    private void FillStuffDetail(StuffGroup index)
    {
        int i = 0;
        Stuff[] stuffInGroup = InteriorMap.StuffGroups[index];
        foreach (Transform t in this.StuffFields.StuffButtonsParent)
        {
            if (i < stuffInGroup.Length)
            {
                Stuff aStuff = stuffInGroup[i];

                if ((int)aStuff < StuffFields.Prefabs.Length)
                {
                    t.gameObject.SetActive(true);
                    t.GetChild(0).GetComponent<Text>().text = aStuff.ToString();
                    t.GetChild(1).GetComponent<Image>().sprite = aStuff.Sprite();
                }
                else
                {
                    t.gameObject.SetActive(false);
                }
            }
            else
            {
                t.gameObject.SetActive(false);
            }
            i++;
        }
    }

    public void SelectStuffToBuild(int index)
    {
        this.StuffFields.StuffGroupDetailPanel.gameObject.SetActive(false);
        ToggleStuffPanel(false);
        this.CurrentStuffBuild = (Stuff)index;
    }

    internal Transform GetPrefab(out Material matchingMaterial)
    {
        return GetPrefab(GuiBridge.Instance.selectedFloorplanGroup, GuiBridge.Instance.selectedFloorplanSubgroup, GuiBridge.Instance.selectedFloorplanMaterial, out matchingMaterial);
    }

    internal Transform GetPrefab(FloorplanGroup g, FloorplanSubGroup s, FloorplanMaterial mat, out Material matchingMaterial)
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

public static class InteriorExtensions
{
    internal static Sprite Sprite(this Stuff s)
    {
        return FloorplanBridge.Instance.StuffFields.Sprites[(int)s];
    }
}
