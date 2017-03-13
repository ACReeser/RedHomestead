using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Interiors;
using UnityEngine.UI;
using RedHomestead.Buildings;

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

    protected virtual Transform[] GetPrefabs()
    {
        return Prefabs;
    }

    public void FillDetail(G group)
    {
        Transform[] allPrefabs = GetPrefabs();
        int i = 0;
        C[] children = Map[group];
        foreach (Transform t in this.DetailButtonsParent)
        {
            if (i < children.Length)
            {
                C child = children[i];

                int index = Convert.ToInt32(child);
                if (index < allPrefabs.Length && allPrefabs[index] != null)
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
public class FloorplanPrefabs: InteriorFields<FloorplanGroup, Floorplan>
{
    public Material[] Materials;
    internal Material SelectedMaterial;

    protected override Dictionary<FloorplanGroup, Floorplan[]> Map
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

[Serializable]
public class ModuleFields : InteriorFields<ConstructionGroup, Module>
{
    protected override Dictionary<ConstructionGroup, Module[]> Map
    {
        get
        {
            return Construction.ConstructionGroupmap;
        }
    }

    protected override Transform[] GetPrefabs()
    {
        return ModuleBridge.Instance.Modules;
    }
}

public class FloorplanBridge : MonoBehaviour {
    public static FloorplanBridge Instance;
    
    public Transform FloorPrefab, MeshFloorPrefab, WallPrefab, SingleCornerColumnPrefab, EdgeColumnPrefab, DoorPrefab;
    public Material ConcreteMaterial;
    public FloorplanPrefabs Floorplans;
    public StuffFields StuffFields;
    public ModuleFields ModuleFields;

	// Use this for initialization
	void Awake () {
        Instance = this;
        ToggleStuffPanel(false);
        ToggleFloorplanPanel(false);
        ToggleModulePanel(false);
    }

    internal void ToggleStuffPanel(bool state)
    {
        this.StuffFields.Toggle(state);
    }

    internal void ToggleFloorplanPanel(bool state)
    {
        this.Floorplans.Toggle(state);
    }

    internal void ToggleModulePanel(bool state)
    {
        this.ModuleFields.Toggle(state);
    }

    public void SelectStuffGroup(int index)
    {
        this.StuffFields.SelectGroup(index);
    }

    public void SelectFloorplanGroup(int index)
    {
        this.Floorplans.SelectGroup(index);
    }

    public void SelectModuleGroup(int index)
    {
        this.ModuleFields.SelectGroup(index);
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
        Floorplan whatToBuild = (Floorplan)Enum.Parse(typeof(Floorplan), UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);

        this.Floorplans.GroupDetailPanel.gameObject.SetActive(false);
        ToggleFloorplanPanel(false);

        PlayerInput.Instance.PlanFloor(whatToBuild);
        //code smell! :(
        PlayerInput.Instance.FPSController.FreezeLook = false;
    }

    public void SelectModuleToBuild()
    {
        Module whatToBuild = (Module)Enum.Parse(typeof(Module), UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);

        this.ModuleFields.GroupDetailPanel.gameObject.SetActive(false);
        ToggleModulePanel(false);

        PlayerInput.Instance.PlanModule(whatToBuild);
        //code smell! :(
        PlayerInput.Instance.FPSController.FreezeLook = false;
    }

    internal Transform GetPrefab(Floorplan s)
    {
        switch (s)
        {
            case Floorplan.SolidFloor:
                return FloorPrefab;
            case Floorplan.MeshFloor:
                return FloorPrefab;

            case Floorplan.SolidWall:
                return WallPrefab;
            case Floorplan.MeshWall:
                return WallPrefab;
            case Floorplan.Door:
                return DoorPrefab;
            case Floorplan.Window:
                return WallPrefab;
            case Floorplan.SingleColumnWall:
                return EdgeColumnPrefab;
            case Floorplan.DoubleColumnWall:
                return EdgeColumnPrefab;

            case Floorplan.Column:
                return SingleCornerColumnPrefab;
        }

        return null;
    }
}

public static class InteriorExtensions
{
    internal static Sprite Sprite(this Stuff s)
    {
        return FloorplanBridge.Instance.StuffFields.Sprites[(int)s];
    }

    internal static Sprite Sprite(this Floorplan s)
    {
        return FloorplanBridge.Instance.Floorplans.Sprites[(int)s];
    }

    internal static Sprite Sprite(this Module m)
    {
        return FloorplanBridge.Instance.ModuleFields.Sprites[(int)m];
    }
}
