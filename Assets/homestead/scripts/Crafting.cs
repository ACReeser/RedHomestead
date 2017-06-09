using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Crafting
{
    public enum Craftable
    {
        Unspecified = -1,
        PowerCube = 0,
        GasCrate,
        WaterCrate,
        SolarPanel,
        IceDrill,
        Pump
    }

    public enum CraftableGroup
    {
        Power, Storage, Extraction
    }

    public interface IBlueprintDetailable
    {
        string Description { get; }
        int BuildTime { get; }
        List<ResourceEntry> Requirements { get; }
        int? PowerSteady { get; }
        int? PowerMin { get; }
        int? PowerMax{ get; }
        int? EnergyStorage { get; }
        Dictionary<Matter, float> IO { get; }
    }

    public abstract class BlueprintData : IBlueprintDetailable
    {
        public string Description { get; set; }
        public int BuildTime { get; set; }
        public List<ResourceEntry> Requirements { get; set; }
        public int? PowerSteady { get; set; }
        public int? PowerMin { get; set; }
        public int? PowerMax { get; set; }
        public int? EnergyStorage { get; set; }
        public Dictionary<Matter, float> IO { get; set; }
        public Matter StorageType = Matter.Unspecified;
        public int? Storage;
    }

    public class CraftingData: BlueprintData
    {
        public CraftingData()
        {
            BuildTime = Crafting.DefaultCraftTimeSeconds;
        }
    }

    public static class Crafting
    {
        public const int DefaultCraftTimeSeconds = 20;

        public static Dictionary<Craftable, CraftingData> CraftData = new Dictionary<Craftable, CraftingData>()
        {
            {
                Craftable.SolarPanel, new CraftingData()
                {
                    Requirements = new List<ResourceEntry>()
                    {
                        new ResourceEntry(.25f, Matter.Steel),
                        new ResourceEntry(.25f, Matter.Copper),
                        new ResourceEntry(.5f, Matter.SiliconWafers)
                    },
                    Description = "A portable solar panel that generates free energy, but only when the sun is shining and the sky is clear.",
                    PowerMin = 0,
                    PowerMax = 3
                }
            },
            {
                Craftable.PowerCube, new CraftingData()
                {
                    Requirements = new List<ResourceEntry>()
                    {
                        new ResourceEntry(.25f, Matter.Steel),
                        new ResourceEntry(.5f, Matter.Aluminium),
                        new ResourceEntry(.5f, Matter.Glass)
                    },
                    Description = "A portable battery pack that stores energy.",
                    EnergyStorage = 1
                }
            },
            {
                Craftable.WaterCrate, new CraftingData()
                {
                    Requirements = new List<ResourceEntry>()
                    {
                        new ResourceEntry(.25f, Matter.Steel),
                        new ResourceEntry(.25f, Matter.Polyethylene)
                    },
                    Description = "A portable water vessel.",
                    Storage = 1,
                    StorageType = Matter.Water
                }
            },
            {
                Craftable.GasCrate, new CraftingData()
                {
                    Requirements = new List<ResourceEntry>()
                    {
                        new ResourceEntry(.5f, Matter.Steel)
                    },
                    Description = "A portable vessel for all types of gasses.",
                    Storage = 1,
                    StorageType = Matter.Hydrogen
                }
            },
            {
                Craftable.Pump, new CraftingData()
                {
                    Requirements = new List<ResourceEntry>()
                    {
                        new ResourceEntry(.25f, Matter.Steel),
                        new ResourceEntry(.25f, Matter.Copper)
                    },
                    Description = "A portable pump to fill and drain gas and water vessels.",
                }
            },
            {
                Craftable.IceDrill, new CraftingData()
                {
                    Requirements = new List<ResourceEntry>()
                    {
                        new ResourceEntry(.25f, Matter.Steel),
                        new ResourceEntry(.25f, Matter.Copper)
                    },
                    Description = "A portable drill to mine water ice from deposits.",
                    PowerSteady = 1
                }
            }
        };

        public static Dictionary<CraftableGroup, Craftable[]> CraftableGroupMap = new Dictionary<CraftableGroup, Craftable[]>()
        {
            {
                CraftableGroup.Power,
                new Craftable[]
                {
                    Craftable.PowerCube,
                    Craftable.SolarPanel
                }
            },
            {
                CraftableGroup.Storage,
                new Craftable[]
                {
                    Craftable.WaterCrate,
                    Craftable.GasCrate,
                    Craftable.Pump
                }
            },
            {
                CraftableGroup.Extraction,
                new Craftable[]
                {
                    Craftable.IceDrill,
                }
            },
        };

        public static Sprite AtlasSprite(this Craftable craft)
        {
            return IconAtlas.Instance.CraftableIcons[Convert.ToInt32(craft)];
        }

        public static Transform Prefab(this Craftable craft)
        {
            return EconomyManager.Instance.CraftablePrefabs[Convert.ToInt32(craft)];
        }
    }
}