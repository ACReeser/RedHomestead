using UnityEngine;
using System.Collections;
using RedHomestead.Construction;
using System.Collections.Generic;
using System;
using System.Linq;

namespace RedHomestead.Construction
{
    public enum Module { Unspecified, SolarPanelSmall }
    //todo: resource could be flags to allow quick "is this in requirements", only if 64 or less resources tho
    public enum Resource { Steel, SiliconWafers }

    public class ResourceEntry
    {
        public Resource Type { get; set; }
        public int Count { get; set; }

        public ResourceEntry(int count, Resource type)
        {
            this.Type = type;
            this.Count = count;
        }
    }

    public static class Requirements
    {
        //todo: load from file probably
        //todo: disallow duplicate resource types by using another dict instead of a list
        public static Dictionary<Module, List<ResourceEntry>> Map = new Dictionary<Module, List<ResourceEntry>>()
        {
            { Module.SolarPanelSmall, new List<ResourceEntry>()
                {
                    new ResourceEntry(2, Resource.Steel),
                    new ResourceEntry(4, Resource.SiliconWafers)
                }
            }
        };
    }
}

public class ConstructionZone : MonoBehaviour {
    public Module UnderConstruction;
    public Transform ModulePrefab;
    public int Progress = 0;

    internal static ConstructionZone CurrentZone;
    internal Dictionary<Resource, int> ResourceCount;
    internal List<ResourceComponent> ResourceList;
    internal bool CanConstruct { get; private set; }
    internal Resource[] RequiredResourceMask;

	// Use this for initialization
	void Start () {
        InitializeRequirements();
	}
	
	// Update is called once per frame
	//void Update () {
	//
	//}
    
    public void InitializeRequirements()
    {
        if (UnderConstruction != Module.Unspecified)
        {
            ResourceCount = new Dictionary<Resource, int>();
            ResourceList = new List<ResourceComponent>();
            //todo: change to requirements.map[underconstruction].keys when that's a dict of <resource, entry> and not a list
            RequiredResourceMask = new Resource[Requirements.Map[this.UnderConstruction].Count];

            int i = 0;
            foreach(ResourceEntry required in Requirements.Map[this.UnderConstruction])
            {
                ResourceCount[required.Type] = 0;
                RequiredResourceMask[i] = required.Type;
                i++;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (ResourceCount != null)
        {
            if (other.CompareTag("Player"))
            {
                GuiBridge.Instance.ShowConstruction(Requirements.Map[this.UnderConstruction], ResourceCount, this.UnderConstruction);
                CurrentZone = this;
            }
            else if (other.CompareTag("movable"))
            {
                ResourceComponent addedResources = other.GetComponent<ResourceComponent>();
                //todo: bug: adds resources that aren't required, and surplus resources
                if (addedResources != null && !addedResources.IsInConstructionZone)
                {
                    ResourceCount[addedResources.ResourceType] += addedResources.Quantity;
                    ResourceList.Add(addedResources);
                    addedResources.IsInConstructionZone = true;
                    RefreshCanConstruct();
                    GuiBridge.Instance.ShowConstruction(Requirements.Map[this.UnderConstruction], ResourceCount, this.UnderConstruction);
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (ResourceCount != null)
        {
            if (other.CompareTag("Player"))
            {
                GuiBridge.Instance.HideConstruction();
                CurrentZone = null;
            }
            else if (other.CompareTag("movable"))
            {
                ResourceComponent removedResources = other.GetComponent<ResourceComponent>();
                //todo: bug: removes resources that aren't required, and surplus resources
                if (removedResources != null && removedResources.IsInConstructionZone)
                {
                    ResourceCount[removedResources.ResourceType] -= removedResources.Quantity;
                    ResourceList.Remove(removedResources);
                    removedResources.IsInConstructionZone = false;
                    RefreshCanConstruct();
                    GuiBridge.Instance.ShowConstruction(Requirements.Map[this.UnderConstruction], ResourceCount, this.UnderConstruction);
                }
            }
        }
    }

    private void RefreshCanConstruct()
    {
        foreach(ResourceEntry resourceEntry in Requirements.Map[this.UnderConstruction])
        {
            if (ResourceCount[resourceEntry.Type] < resourceEntry.Count)
            {
                print("missing " + (resourceEntry.Count - ResourceCount[resourceEntry.Type]) + " " + resourceEntry.Type.ToString());
                CanConstruct = false;
                break;
            }
        }
        CanConstruct = true;
    }

    public void WorkOnConstruction()
    {
        this.Progress = 100;
        this.Complete();
    }

    public void Complete()
    {
        //todo: move player out of the way
        GameObject.Instantiate(ModulePrefab, this.transform.position, this.transform.rotation);
        if (CurrentZone == this)
        {
            CurrentZone = null;
            GuiBridge.Instance.HideConstruction();
        }
        //todo: make this more efficient
        Dictionary<Resource, int> deletedCount = new Dictionary<Resource, int>();
        for(int i = this.ResourceList.Count - 1; i >= 0; i--)
        {
            ResourceComponent component = this.ResourceList[i];

            if (RequiredResourceMask.Contains(component.ResourceType))
            {
                int numDeleted = 0;
                if (deletedCount.ContainsKey(component.ResourceType))
                {
                    numDeleted = deletedCount[component.ResourceType];
                    deletedCount[component.ResourceType] = 0;
                }

                if (numDeleted < Requirements.Map[this.UnderConstruction].Where(r => r.Type == component.ResourceType).Count())
                {
                    this.ResourceList.Remove(component);
                    Destroy(component.gameObject);
                }
            }
        }
        Destroy(this.gameObject);
    }
}
