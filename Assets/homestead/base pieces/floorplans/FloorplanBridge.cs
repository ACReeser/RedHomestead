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
        this.GroupDetailPanel.gameObject.SetActive(this.ShowGroupDetailOnToggle(state));

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

    protected virtual bool ShowGroupDetailOnToggle(bool state)
    {
        return false;
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
    public RectTransform DetailCurrentCraftingParent, DetailParent;
    public Text Progress;

    public override Dictionary<CraftableGroup, Craftable[]> Map
    {
        get
        {
            return Crafting.CraftableGroupMap;
        }
    }

    private bool DetailState;
    internal void Toggle(bool overallState, bool detailState)
    {
        DetailState = detailState;
        Toggle(overallState);
        GroupsPanel.gameObject.SetActive(!detailState);
        DetailCurrentCraftingParent.gameObject.SetActive(detailState);
    }

    protected override bool ShowGroupDetailOnToggle(bool state)
    {
        if (state)
        {
            return this.DetailState;
        }
        else
        {
            return false;
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
        ToggleCraftablePanel(false, false);
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

    internal void ToggleCraftablePanel(bool overallState, bool detailState)
    {
        this.CraftableFields.Toggle(overallState, detailState);
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
        SelectThing(CraftableFields, null, PlayerInput.Instance.PlanCraftable);
    }

    private void SelectThing<G, C>(InteriorFields<G, C> fields, Action<bool> toggleOff, Action<C> plan) where C : IConvertible
    {
        C whatToBuild = (C)Enum.Parse(typeof(C), UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject.name);

        if (toggleOff != null)
        {
            fields.GroupDetailPanel.gameObject.SetActive(false);
            toggleOff(false);
        }

        plan(whatToBuild);

        if (toggleOff != null)
        {
            //code smell! :(
            PlayerInput.Instance.FPSController.FreezeLook = false;
        }
    }

    private Craftable hoverCraftable;
    public void HoverCraftableButton(int index)
    {
        Hover(hoverCraftable, CraftableFields, index, Crafting.CraftData, (current) => this.hoverCraftable = current);
    }

    private Module currentDetail = Module.Unspecified;
    public void HoverModuleButton(int index)
    {
        Hover(currentDetail, ModuleFields, index, Construction.BuildData, (current) => this.currentDetail = current);
    }

    private static void Hover<G, C, D>(C currentChild, InteriorFields<G, C> fields, int index, Dictionary<C, D> buildData, Action<C> assignCurrentChild) where C : IConvertible where D : BlueprintData
    {
        print(String.Format("{0} {1}", fields.CurrentGroup, index));
        C whatToBuild = fields.Map[fields.CurrentGroup][index];

        Hover(currentChild, whatToBuild, fields, buildData, assignCurrentChild);
    }

    private static void Hover<G, C, D>(C currentChild, C whatToBuild, InteriorFields<G, C> fields, Dictionary<C, D> buildData, Action<C> assignCurrentChild) where C : IConvertible where D : BlueprintData
    {
        //avoid filling every frame that we're hovered
        if (Convert.ToInt32(whatToBuild) != Convert.ToInt32(currentChild))
        {
            if (buildData.ContainsKey(whatToBuild))
            {
                assignCurrentChild(whatToBuild);

                BlueprintData data = buildData[whatToBuild];

                fields.DetailHeader.text = whatToBuild.ToString();
                fields.DetailDescription.text = data.Description;
                int i = 0;
                foreach(RectTransform text in fields.DetailMaterialsParent)
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

                fields.DetailPower.transform.parent.gameObject.SetActive(data.PowerSteady.HasValue || data.PowerMin.HasValue);

                if (data.PowerSteady.HasValue)
                    fields.DetailPower.text = data.PowerSteady.Value > 0 ? "+" + data.PowerSteady.Value : data.PowerSteady.Value.ToString();
                else if (data.PowerMin.HasValue && data.PowerMin.Value >= 0)
                    fields.DetailPower.text = String.Format("+[{0}-{1}]", data.PowerMin.Value, data.PowerMax.Value);

                fields.DetailPowerStorage.transform.parent.gameObject.SetActive(data.EnergyStorage.HasValue);
                if (data.EnergyStorage.HasValue)
                    fields.DetailPower.text = data.EnergyStorage.ToString();

                fields.DetailTypedStorage.transform.parent.gameObject.SetActive(data.StorageType != Matter.Unspecified);
                if (data.StorageType != Matter.Unspecified)
                {
                    fields.DetailStorage.transform.parent.gameObject.SetActive(false);
                    fields.DetailTypedStorage.text = data.Storage.Value.ToString();
                    if (data.StorageType == Matter.Methane)
                        fields.DetailTypedStorageSprite.sprite = GuiBridge.Instance.Icons.MiscIcons[(int)MiscIcon.Molecule];
                    else
                        fields.DetailTypedStorageSprite.sprite = data.StorageType.Sprite();
                }
                else
                {
                    fields.DetailStorage.transform.parent.gameObject.SetActive(data.Storage.HasValue);
                    if (data.Storage.HasValue)
                        fields.DetailStorage.text = data.Storage.Value.ToString();
                }

                if (data.IO == null)
                {
                    foreach(Text io in fields.DetailIO)
                    {
                        io.transform.parent.gameObject.SetActive(false);
                    }
                }
                else
                {
                    i = 0;
                    foreach(KeyValuePair<Matter, float> entry in data.IO)
                    {
                        fields.DetailIO[i].transform.parent.gameObject.SetActive(true);
                        fields.DetailIO[i].text = entry.Value > 0 ? "+" + entry.Value : entry.Value.ToString();
                        fields.DetailIO[i].transform.parent.GetChild(0).GetComponent<Image>().sprite = entry.Key.Sprite();
                        i++;
                    }
                    for(int j = i;  j < fields.DetailIO.Length; j++)
                    {
                        fields.DetailIO[i].transform.parent.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    internal void SetCurrentCraftableDetail(Craftable craftable, float? progress = null)
    {
        if (craftable != Craftable.Unspecified)
        {
            Hover(Craftable.Unspecified, craftable, CraftableFields, Crafting.CraftData, (current) => { });
            UpdateDetailCraftableProgressView(craftable, progress);
        }

        bool showingDetail = craftable != Craftable.Unspecified;

        CraftableFields.DetailCurrentCraftingParent.gameObject.SetActive(showingDetail);
        CraftableFields.DetailParent.offsetMin = new Vector2(showingDetail ? 0f : 200f, 0);
        CraftableFields.DetailParent.offsetMax = new Vector2(showingDetail ? -200f : 0f, 0);
        CraftableFields.DetailButtonsParent.gameObject.SetActive(!showingDetail);
    }

    public void UpdateDetailCraftableProgressView(Craftable craftable, float? progress)
    {
        string progressPercentageString = String.Format("{0:0.#}%", progress.Value * 100f);
        int completionHours = Crafting.CraftData[craftable].BuildTime;
        int currentHours = Mathf.FloorToInt(progress.Value * completionHours);

        CraftableFields.Progress.text = String.Format("<b>{0}</b>\n{1}/{2} Hrs", progressPercentageString, currentHours, completionHours);
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
