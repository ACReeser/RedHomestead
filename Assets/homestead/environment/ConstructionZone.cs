using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using RedHomestead.Simulation;
using RedHomestead.Buildings;
using RedHomestead.Persistence;

[Serializable]
public class ResourceCountDictionary: SerializableDictionary<Matter, int> { }

[Serializable]
public class ConstructionData: FacingData
{
    public Module ModuleTypeUnderConstruction;
    public Dictionary<Matter, int> ResourceCount;
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

    public void Initialize(Module toBuild)
    {
        Data.ModuleTypeUnderConstruction = toBuild;
        ModulePrefab = PrefabCache<Module>.Cache.GetPrefab(toBuild);

        InitializeRequirements();
    }
    
    public void InitializeRequirements()
    {
        if (Data.ModuleTypeUnderConstruction != Module.Unspecified)
        {
            Data.ResourceCount = new Dictionary<Matter, int>();
            ResourceList = new List<ResourceComponent>();
            //todo: change to Construction.Requirements[underconstruction].keys when that's a dict of <resource, entry> and not a list
            RequiredResourceMask = new Matter[Construction.Requirements[Data.ModuleTypeUnderConstruction].Count];

            int i = 0;
            foreach(ResourceEntry required in Construction.Requirements[Data.ModuleTypeUnderConstruction])
            {
                Data.ResourceCount[required.Type] = 0;
                RequiredResourceMask[i] = required.Type;
                i++;
            }

            //todo: move the pylons and tape to match the width/length of the module to be built
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (Data.ResourceCount != null)
        {
            if (other.CompareTag("Player"))
            {
                GuiBridge.Instance.ShowConstruction(Construction.Requirements[Data.ModuleTypeUnderConstruction], Data.ResourceCount, Data.ModuleTypeUnderConstruction);
                ZoneThatPlayerIsStandingIn = this;
            }
            else if (other.CompareTag("movable"))
            {
                ResourceComponent addedResources = other.GetComponent<ResourceComponent>();
                //todo: bug: adds resources that aren't required, and surplus resources
                if (addedResources != null && !addedResources.IsInConstructionZone)
                {
#warning rounding error
                    Data.ResourceCount[addedResources.Data.ResourceType] += (int)addedResources.Data.Quantity;
                    ResourceList.Add(addedResources);
                    addedResources.IsInConstructionZone = true;
                    RefreshCanConstruct();
                    GuiBridge.Instance.ShowConstruction(Construction.Requirements[Data.ModuleTypeUnderConstruction], Data.ResourceCount, Data.ModuleTypeUnderConstruction);
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
                //todo: bug: removes resources that aren't required, and surplus resources
                if (removedResources != null && removedResources.IsInConstructionZone)
                {
#warning rounding error
                    Data.ResourceCount[removedResources.Data.ResourceType] -= (int)removedResources.Data.Quantity;
                    ResourceList.Remove(removedResources);
                    removedResources.IsInConstructionZone = false;
                    RefreshCanConstruct();
                    GuiBridge.Instance.ShowConstruction(Construction.Requirements[Data.ModuleTypeUnderConstruction], Data.ResourceCount, Data.ModuleTypeUnderConstruction);
                }
            }
        }
    }

    private void RefreshCanConstruct()
    {
        CanConstruct = true;

        foreach(ResourceEntry resourceEntry in Construction.Requirements[Data.ModuleTypeUnderConstruction])
        {
            if (Data.ResourceCount[resourceEntry.Type] < resourceEntry.Count)
            {
                print("missing " + (resourceEntry.Count - Data.ResourceCount[resourceEntry.Type]) + " " + resourceEntry.Type.ToString());
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
        //todo: move player out of the way
        //actually, we _should_ only be able to complete construction when the player
        //is outside the zone looking in, so maybe not
        
        GameObject.Instantiate(ModulePrefab, this.transform.position, this.transform.rotation);
        if (ZoneThatPlayerIsStandingIn == this)
        {
            ZoneThatPlayerIsStandingIn = null;
            GuiBridge.Instance.HideConstruction();
        }

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

            if (RequiredResourceMask.Contains(component.Data.ResourceType))
            {
                int numDeleted = 0;
                if (deletedCount.ContainsKey(component.Data.ResourceType))
                {
                    numDeleted = deletedCount[component.Data.ResourceType];
                    deletedCount[component.Data.ResourceType] = 0;
                }

                if (numDeleted < Construction.Requirements[Data.ModuleTypeUnderConstruction].Where(r => r.Type == component.Data.ResourceType).Count())
                {
                    this.ResourceList.Remove(component);
                    Destroy(component.gameObject);
                }
            }
        }

        Destroy(this.gameObject);
    }
}
