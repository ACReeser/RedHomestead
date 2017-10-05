using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Persistence;

[Serializable]
public class ResourceCountDictionary: SerializableDictionary<Matter, float> { }

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
            foreach(ResourceVolumeEntry required in buildingData.Requirements)
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
                //todo: bug: adds surplus resources
                if (addedResources != null && !addedResources.IsInConstructionZone && Data.ResourceCount.ContainsKey(addedResources.data.Container.MatterType))
                {
#warning rounding error
                    Data.ResourceCount[addedResources.Data.Container.MatterType] += (int)addedResources.Data.Container.CurrentAmount;
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
#warning rounding error
                    Data.ResourceCount[removedResources.Data.Container.MatterType] -= (int)removedResources.Data.Container.CurrentAmount;
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

        foreach(ResourceVolumeEntry resourceEntry in Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements)
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

        //todo: make this more efficient
        //what this code is doing:
        //only destroying those entries in the ResourceList that are required to build the Module
        //so you can't put in 100 steel to something that requires 10 and lose 90 excess steel
        Dictionary<Matter, int> deletedCount = new Dictionary<Matter, int>();
        for(int i = this.ResourceList.Count - 1; i >= 0; i--)
        {
            ResourceComponent component = this.ResourceList[i];
            //tell the component it isn't in a construction zone
            //just in case it will live through the rest of this method
            //(this frees it for use in another zone)
            component.IsInConstructionZone = false;

            if (RequiredResourceMask.Contains(component.Data.Container.MatterType))
            {
                int numDeleted = 0;
                if (deletedCount.ContainsKey(component.Data.Container.MatterType))
                {
                    numDeleted = deletedCount[component.Data.Container.MatterType];
                    deletedCount[component.Data.Container.MatterType] = 0;
                }

                if (numDeleted < Construction.BuildData[Data.ModuleTypeUnderConstruction].Requirements.Where(r => r.Type == component.Data.Container.MatterType).Count())
                {
                    this.ResourceList.Remove(component);
                    Destroy(component.gameObject);
                }
            }
        }

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
}
