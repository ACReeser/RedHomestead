using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Interiors;
using UnityEngine.UI;
using RedHomestead.Buildings;
using RedHomestead.Simulation;
using RedHomestead.Crafting;

[Serializable]
public abstract class InteriorFields<G, C> where C : IConvertible
{
    public RectTransform FullPanel, GroupsPanel, GroupDetailPanel, DetailButtonsParent, DetailMaterialsParent;
    public Transform[] Prefabs;
    public Sprite[] Sprites;
    public Text DetailHeader, DetailDescription, DetailPower, DetailPowerStorage, DetailStorage, DetailTypedStorage;
    public Text[] DetailIO;
    public Image DetailTypedStorageSprite;
    public G CurrentGroup { get; private set; }

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
        this.CurrentGroup = (G)(object)index;
        this.GroupsPanel.gameObject.SetActive(false);
        FillDetail(this.CurrentGroup);
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

    public abstract Dictionary<G, C[]> Map { get; }
}

[Serializable]
public class FloorplanPrefabs: InteriorFields<FloorplanGroup, Floorplan>
{
    public Material[] Materials;
    internal Material SelectedMaterial;

    public override Dictionary<FloorplanGroup, Floorplan[]> Map
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
    public override Dictionary<StuffGroup, Stuff[]> Map
    {
        get
        {
            return InteriorMap.StuffGroups;
        }
    }
}

[Serializable]
public class CraftableFields : InteriorFields<CraftableGroup, Craftable>
{
    public override Dictionary<CraftableGroup, Craftable[]> Map
    {
        get
        {
            return Crafting.CraftableGroupMap;
        }
    }
}

[Serializable]
public class ModuleFields : InteriorFields<ConstructionGroup, Module>
{
    public override Dictionary<ConstructionGroup, Module[]> Map
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
    public CraftableFields CraftableFields;

    // Use this for initialization
    void Awake () {
        Instance = this;
        ToggleStuffPanel(false);
        ToggleFloorplanPanel(false);
        ToggleModulePanel(false);
        ToggleCraftablePanel(false);
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

    internal void ToggleCraftablePanel(bool state)
    {
        this.CraftableFields.Toggle(state);
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

    public void SelectCraftableGroup(int index)
    {
        this.CraftableFields.SelectGroup(index);
    }

    public void SelectStuffToBuild()
    {
        SelectThing(StuffFields, ToggleStuffPanel, PlayerInput.Instance.PlanStuff);
    }

    public void SelectFloorplanToBuild()
    {
        SelectThing(Floorplans, ToggleFloorplanPanel, PlayerInput.Instance.PlanFloor);
    }

    public void SelectModuleToBuild()
    {
        SelectThing(ModuleFields, ToggleModulePanel, PlayerInput.Instance.PlanModule);
    }

    public void SelectCraftableToBuild()
    {
        SelectThing(CraftableFields, ToggleCraftablePanel, PlayerInput.Instance.PlanCraftable);
    }

    private void SelectThing<G, C>(InteriorFields<G, C> fields, Action<bool> toggle, Action<C> plan) where C : IConvertible
    {
        C whatToBuild = (C)Enum.Parse(typeof(C), UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);

        fields.GroupDetailPanel.gameObject.SetActive(false);
        toggle(false);

        plan(whatToBuild);
        //code smell! :(
        PlayerInput.Instance.FPSController.FreezeLook = false;
    }

    private Craftable currentCraftable;
    public void HoverCraftableButton(int index)
    {
        Craftable whatToBuild = this.CraftableFields.Map[this.CraftableFields.CurrentGroup][index];

        //avoid filling every frame that we're hovered
        if (whatToBuild != this.currentCraftable)
        {
            if (Crafting.CraftData.ContainsKey(whatToBuild))
            {
                this.currentCraftable = whatToBuild;

            }
        }
    }

    private Module currentDetail = Module.Unspecified;
    public void HoverModuleButton(int index)
    {
        Module whatToBuild = this.ModuleFields.Map[this.ModuleFields.CurrentGroup][index];

        //avoid filling every frame that we're hovered
        if (whatToBuild != this.currentDetail)
        {
            if (Construction.BuildData.ContainsKey(whatToBuild))
            {
                this.currentDetail = whatToBuild;
                BuildingData data = Construction.BuildData[whatToBuild];
                ModuleFields.DetailHeader.text = whatToBuild.ToString();
                ModuleFields.DetailDescription.text = data.Description;
                int i = 0;
                foreach(RectTransform text in ModuleFields.DetailMaterialsParent)
                {
                    if (i == 0)
                    {
                        //noop;
                    }
                    else
                    {
                        int actualIndex = i - 1;
                        bool active = actualIndex < data.Requirements.Count;
                        text.gameObject.SetActive(active);
                        if (active)
                        {
                            text.GetComponent<Text>().text = data.Requirements[actualIndex].ToString();
                            text.GetChild(0).GetComponent<Image>().sprite = data.Requirements[actualIndex].Type.Sprite();
                        }
                    }

                    i++;
                }

                ModuleFields.DetailPower.transform.parent.gameObject.SetActive(data.PowerSteady.HasValue || data.PowerMin.HasValue);

                if (data.PowerSteady.HasValue)
                    ModuleFields.DetailPower.text = data.PowerSteady.Value > 0 ? "+" + data.PowerSteady.Value : data.PowerSteady.Value.ToString();
                else if (data.PowerMin.HasValue && data.PowerMin.Value >= 0)
                    ModuleFields.DetailPower.text = String.Format("+[{0}-{1}]", data.PowerMin.Value, data.PowerMax.Value);

                ModuleFields.DetailPowerStorage.transform.parent.gameObject.SetActive(data.EnergyStorage.HasValue);
                if (data.EnergyStorage.HasValue)
                    ModuleFields.DetailPower.text = data.EnergyStorage.ToString();

                ModuleFields.DetailTypedStorage.transform.parent.gameObject.SetActive(data.StorageType != Matter.Unspecified);
                if (data.StorageType != Matter.Unspecified)
                {
                    ModuleFields.DetailStorage.transform.parent.gameObject.SetActive(false);
                    ModuleFields.DetailTypedStorage.text = data.Storage.Value.ToString();
                    if (data.StorageType == Matter.Methane)
                        ModuleFields.DetailTypedStorageSprite.sprite = GuiBridge.Instance.Icons.MiscIcons[(int)MiscIcon.Molecule];
                    else
                        ModuleFields.DetailTypedStorageSprite.sprite = data.StorageType.Sprite();
                }
                else
                {
                    ModuleFields.DetailStorage.transform.parent.gameObject.SetActive(data.Storage.HasValue);
                    if (data.Storage.HasValue)
                        ModuleFields.DetailStorage.text = data.Storage.Value.ToString();
                }

                if (data.IO == null)
                {
                    foreach(Text io in ModuleFields.DetailIO)
                    {
                        io.transform.parent.gameObject.SetActive(false);
                    }
                }
                else
                {
                    i = 0;
                    foreach(KeyValuePair<Matter, float> entry in data.IO)
                    {
                        ModuleFields.DetailIO[i].transform.parent.gameObject.SetActive(true);
                        ModuleFields.DetailIO[i].text = entry.Value > 0 ? "+" + entry.Value : entry.Value.ToString();
                        ModuleFields.DetailIO[i].transform.parent.GetChild(0).GetComponent<Image>().sprite = entry.Key.Sprite();
                        i++;
                    }
                    for(int j = i;  j < ModuleFields.DetailIO.Length; j++)
                    {
                        ModuleFields.DetailIO[i].transform.parent.gameObject.SetActive(false);
                    }
                }
            }
        }
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
