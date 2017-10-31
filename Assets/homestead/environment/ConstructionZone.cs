using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Persistence;

[Serializable]
public class ResourceUnitCountDictionary: SerializableDictionary<Matter, float>
{
    public IEnumerator<float> SquareMeters(Matter m)
    {
        float amountInUnits;
        if (this.TryGetValue(m, out amountInUnits))
        {
            float volume = amountInUnits * m.CubicMetersPerUnit();
            do
            {

                float thisCrateAmount = Mathf.Min(1f, volume); ;
                volume -= thisCrateAmount;
                yield return thisCrateAmount;
            }
            while (volume > 0f);
        }
    }
}

[Serializable]
public class ConstructionData: FacingData
{
    public Module ModuleTypeUnderConstruction;
    public string DepositInstanceID;
    public Dictionary<Matter, float> ResourceCount;
    public float CurrentProgressSeconds = 0;
}

public class ConstructionZone : MonoBehaviour, IDataContainer<ConstructionData> {
    internal Transform ModulePrefab;

    private float RequiredProgressSeconds = 10f;
    internal float ProgressPercentage
    {
        get
        {
            return Data.CurrentProgressSeconds / RequiredProgressSeconds;
        }
    }

    internal static ConstructionZone ZoneThatPlayerIsStandingIn;
    internal static ConstructionZone LastPlacedZone;

    internal List<ResourceComponent> ResourceList;
    internal bool CanConstruct { get; private set; }

    public ConstructionData data;
    public ConstructionData Data { get { return data; } set { data = value; } }

    internal Matter[] RequiredResourceMask;

	// Use this for initialization
	void Start () {
        InitializeRequirements();
	}

    public void Initialize(Module toBuild, string depositID = null)
    {
        Data.ModuleTypeUnderConstruction = toBuild;
        Data.DepositInstanceID = depositID;
        ModulePrefab = PrefabCache<Module>.Cache.GetPrefab(toBuild);

        InitializeRequirements();
        LastPlacedZone = this;
    }
    
    public void InitializeRequirements()
    {
        if (Data.ModuleTypeUnderConstruction != Module.Unspecified)
        {
            Data.ResourceCount = new Dictionary<Matter, float>();
            BuildingData buildingData = Construction.BuildData[Data.ModuleTypeUnderConstruction];
            RequiredProgressSeconds = buildingData.BuildTimeHours * SunOrbit.GameSecondsPerMartianMinute * 60f;
            ResourceList = new List<ResourceComponent>();
            //todo: change to Construction.Requirements[underconstruction].keys when that's a dict of <resource, entry> and not a list
            RequiredResourceMask = new Matter[buildingData.Requirements.Count];

            int i = 0;
            foreach(IResourceEntry required in buildingData.Requirements)
            {
                Data.ResourceCount[required.Type] = 0;
                RequiredResourceMask[i] = required.Type;
                i++;
            }
            
            int j = 0;
            int radius = Construction.BuildRadius(Data.ModuleTypeUnderConstruction);
            foreach(Transform t in this.transform)
            {
                if (j < 4)//poles
                {
                    t.localPosition = new Vector3(j % 2 == 0 ? -radius: radius, 0f, j < 2 ? -radius : radius);
                }
                else //tape
                {
                    int coeff = j % 2 == 0 ? -1 : 1;
                    t.localPosition = new Vector3(j < 6 ? 0 : radius * coeff, 1f, j > 5 ? 0 : radius * coeff);
                    t.localScale = new Vector3(radius * 2, .2f, .01f);
                }
                j++;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (Data.ResourceCount != null)
        {
            if (other.CompareTag("Player"))
            {
                GuiBridge.Instance.ShowConstruction(Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements, Data.ResourceCount, Data.ModuleTypeUnderConstruction);
                ZoneThatPlayerIsStandingIn = this;
            }
            else if (other.CompareTag("movable"))
            {
                ResourceComponent addedResources = other.GetComponent<ResourceComponent>();
                
                if (addedResources != null && !addedResources.IsInConstructionZone && Data.ResourceCount.ContainsKey(addedResources.data.Container.MatterType))
                {
                    Data.ResourceCount[addedResources.Data.Container.MatterType] += addedResources.Data.Container.CurrentAmount;
                    ResourceList.Add(addedResources);
                    addedResources.IsInConstructionZone = true;
                    RefreshCanConstruct();
                    GuiBridge.Instance.ShowConstruction(Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements, Data.ResourceCount, Data.ModuleTypeUnderConstruction);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (Data.ResourceCount != null)
        {
            if (other.CompareTag("Player"))
            {
                GuiBridge.Instance.HideConstruction();
                ZoneThatPlayerIsStandingIn = null;
            }
            else if (other.CompareTag("movable"))
            {
                ResourceComponent removedResources = other.GetComponent<ResourceComponent>();
                //todo: bug: removes surplus resources
                if (removedResources != null && removedResources.IsInConstructionZone && Data.ResourceCount.ContainsKey(removedResources.data.Container.MatterType))
                {
                    Data.ResourceCount[removedResources.Data.Container.MatterType] -= removedResources.Data.Container.CurrentAmount;
                    ResourceList.Remove(removedResources);
                    removedResources.IsInConstructionZone = false;
                    RefreshCanConstruct();
                    GuiBridge.Instance.ShowConstruction(Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements, Data.ResourceCount, Data.ModuleTypeUnderConstruction);
                }
            }
        }
    }

    private void RefreshCanConstruct()
    {
        CanConstruct = true;

        foreach(IResourceEntry resourceEntry in Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements)
        {
            if (Data.ResourceCount[resourceEntry.Type] < resourceEntry.AmountByVolume)
            {
                print("missing " + (resourceEntry.AmountByVolume - Data.ResourceCount[resourceEntry.Type]) + " " + resourceEntry.Type.ToString());
                CanConstruct = false;
                break;
            }
        }
    }

    public void WorkOnConstruction(float constructionTime)
    {
        if (CanConstruct)
        {
            Data.CurrentProgressSeconds += constructionTime;

            if (Data.CurrentProgressSeconds >= this.RequiredProgressSeconds)
            {
                this.Complete();
            }
        }
    }

    public void Complete()
    {
#warning band-aid fix to stop InitializeStartingData from being called twice
        Game.Current.IsNewGame = false;
        //todo: move player out of the way
        //actually, we _should_ only be able to complete construction when the player
        //is outside the zone looking in, so maybe not

        Transform newT = (Transform)GameObject.Instantiate(ModulePrefab, this.transform.position, this.transform.rotation);

        //link ore extractor to deposit
        if (!String.IsNullOrEmpty(Data.DepositInstanceID))
        {
            OreExtractor drill = newT.GetComponent<OreExtractor>();
            if (drill != null)
            {
                drill.InitializeDeposit(Data.DepositInstanceID);
            }
        }

        newT.GetComponent<IBuildable>().InitializeStartingData();

        if (ZoneThatPlayerIsStandingIn == this)
        {
            ZoneThatPlayerIsStandingIn = null;
            GuiBridge.Instance.HideConstruction();
        }

        Game.Current.Score.ModulesBuilt++;

        //copy the requirements by adding them to a dictionary
        Dictionary<Matter, float> matterToVolumeMap = Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements.ToDictionary(x => x.Type, y => y.AmountByVolume);
        //copy the resource list by toArray it
        ResourceComponent[] components = ResourceList.ToArray();

        //go backwards through the components
        for (int i = components.Length - 1; i > -1; i--)
        {
            ResourceComponent component = components[i];
            float volumeToPull = 0f;
            //if the component is used in the requirements
            if (matterToVolumeMap.TryGetValue(component.Data.Container.MatterType, out volumeToPull) && volumeToPull > 0f)
            {
                //decrease the matterToVolumeMap by as much pull as possible
                matterToVolumeMap[component.Data.Container.MatterType] -= component.Data.Container.Pull(volumeToPull);

                //then if the crate is spent
                if (component.Data.Container.CurrentAmount <= 0f)
                {
                    //delete it
                    Destroy(component.gameObject);
                }

                //if the requirement is met
                if (matterToVolumeMap[component.Data.Container.MatterType] <= 0f)
                {
                    //remove the requirement (to stop the next component from depleting any)
                    matterToVolumeMap.Remove(component.Data.Container.MatterType);
                }
            }
        }
        if (matterToVolumeMap.Keys.Count > 0)
        {
            Debug.LogError("Construction zone built something but had requirements left over!");
        }
        this.ResourceList = null;

        Destroy(this.gameObject);
    }

    internal void Deconstruct()
    {
        if (ResourceList != null && ResourceList.Count > 0)
        {
            foreach(var r in ResourceList)
            {
                r.IsInConstructionZone = false;
            }
        }

        if (ZoneThatPlayerIsStandingIn == this)
        {
            ZoneThatPlayerIsStandingIn = null;
            GuiBridge.Instance.HideConstruction();
        }

        Destroy(this.gameObject);
    }

    void OnDestroy()
    {
        if (this == LastPlacedZone)
            LastPlacedZone = null;

        if (this == ZoneThatPlayerIsStandingIn)
            ZoneThatPlayerIsStandingIn = null;
    }
}
