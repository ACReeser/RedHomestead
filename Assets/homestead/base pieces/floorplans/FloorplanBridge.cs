using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Interiors;
using UnityEngine.UI;

[Serializable]
public abstract class InteriorFields<G, C> where C : IConvertible
{
    public RectTransform FullPanel, GroupsPanel, GroupDetailPanel, DetailButtonsParent;
    public Transform[] Prefabs;
    public Sprite[] Sprites;

    public void Toggle(bool state)
    {
        this.FullPanel.gameObject.SetActive(state);
        this.GroupsPanel.gameObject.SetActive(state);
        this.GroupDetailPanel.gameObject.SetActive(false);

        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = state;
    }

    public void SelectGroup(int index)
    {

        this.GroupsPanel.gameObject.SetActive(false);
        FillDetail((G)(object)index);
        this.GroupDetailPanel.gameObject.SetActive(true);
    }

    public void FillDetail(G group)
    {
        int i = 0;
        C[] children = Map[group];
        foreach (Transform t in this.DetailButtonsParent)
        {
            if (i < children.Length)
            {
                C child = children[i];

                int index = Convert.ToInt32(child);
                if (index < Prefabs.Length && Prefabs[index] != null)
                {
                    t.gameObject.SetActive(true);
                    //cheat and set the name to the enum value;
                    t.name = child.ToString();
                    t.GetChild(0).GetComponent<Text>().text = child.ToString();
                    t.GetChild(1).GetComponent<Image>().sprite = Sprites[index];
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

    protected abstract Dictionary<G, C[]> Map { get; }
}

[Serializable]
public class FloorplanPrefabs: InteriorFields<FloorplanGroup, FloorplanSubGroup>
{
    public Material[] Materials;
    internal Material SelectedMaterial;

    protected override Dictionary<FloorplanGroup, FloorplanSubGroup[]> Map
    {
        get
        {
            return InteriorMap.FloorplanGroupmap;
        }
    }
}

[Serializable]
public class StuffFields: InteriorFields<StuffGroup, Stuff>
{

    protected override Dictionary<StuffGroup, Stuff[]> Map
    {
        get
        {
            return InteriorMap.StuffGroups;
        }
    }
}

public class FloorplanBridge : MonoBehaviour {
    public static FloorplanBridge Instance;
    
    public Transform FloorPrefab, MeshFloorPrefab, WallPrefab, SingleCornerColumnPrefab, EdgeColumnPrefab, DoorPrefab;
    public Material ConcreteMaterial;
    public FloorplanPrefabs Floorplans;
    public StuffFields StuffFields;

	// Use this for initialization
	void Awake () {
        Instance = this;
        ToggleStuffPanel(false);
        ToggleFloorplanPanel(false);
    }

    internal void ToggleStuffPanel(bool state)
    {
        this.StuffFields.Toggle(state);
    }

    internal void ToggleFloorplanPanel(bool state)
    {
        this.Floorplans.Toggle(state);
    }

    public void SelectStuffGroup(int index)
    {
        this.StuffFields.SelectGroup(index);
    }

    public void SelectFloorplanGroup(int index)
    {
        this.Floorplans.SelectGroup(index);
    }

    public void SelectStuffToBuild()
    {
        Stuff whatToBuild = (Stuff)Enum.Parse(typeof(Stuff), UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);

        this.StuffFields.GroupDetailPanel.gameObject.SetActive(false);
        ToggleStuffPanel(false);

        PlayerInput.Instance.PlanStuff(whatToBuild);
        //code smell! :(
        PlayerInput.Instance.FPSController.FreezeLook = false;
    }

    public void SelectFloorplanToBuild()
    {

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

    internal static Sprite Sprite(this FloorplanSubGroup s)
    {
        return FloorplanBridge.Instance.Floorplans.Sprites[(int)s];
    }
}
