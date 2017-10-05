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
        Pump,
        Crate,
        Toolbox
    }

    public enum CraftableGroup
    {
        Power, Storage, Extraction
    }

    public interface IBlueprintDetailable
    {
        string Description { get; }
        int BuildTimeHours { get; }
        List<IResourceEntry> Requirements { get; }
        int? PowerSteady { get; }
        int? PowerMin { get; }
        int? PowerMax{ get; }
        int? EnergyStorage { get; }
        Dictionary<Matter, float> IO { get; }
    }

    public abstract class BlueprintData : IBlueprintDetailable
    {
        public string Description { get; set; }
        public int BuildTimeHours { get; set; }
        public List<IResourceEntry> Requirements { get; set; }
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
            BuildTimeHours = Crafting.DefaultCraftTimeSeconds;
        }
    }
    public class PrinterData : BlueprintData
    {
        private const int DefaultPrintTimeHours = 5;

        public PrinterData()
        {
            BuildTimeHours = DefaultPrintTimeHours;
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
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.Steel),
                        new ResourceVolumeEntry(.25f, Matter.Copper),
                        new ResourceVolumeEntry(.5f, Matter.SiliconWafers)
                    },
                    Description = "A portable solar panel that generates free energy, but only when the sun is shining and the sky is clear.",
                    PowerMin = 0,
                    PowerMax = 3
                }
            },
            {
                Craftable.PowerCube, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.Steel),
                        new ResourceVolumeEntry(.5f, Matter.Aluminium),
                        new ResourceVolumeEntry(.5f, Matter.Glass)
                    },
                    Description = "A portable battery pack that stores energy.",
                    EnergyStorage = 1
                }
            },
            {
                Craftable.WaterCrate, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.Steel),
                        new ResourceVolumeEntry(.25f, Matter.Polyethylene)
                    },
                    Description = "A portable water vessel.",
                    Storage = 1,
                    StorageType = Matter.Water
                }
            },
            {
                Craftable.GasCrate, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.5f, Matter.Steel)
                    },
                    Description = "A portable vessel for all types of gasses.",
                    Storage = 1,
                    StorageType = Matter.Hydrogen
                }
            },
            {
                Craftable.Pump, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.Steel),
                        new ResourceVolumeEntry(.25f, Matter.Copper)
                    },
                    Description = "A portable pump to fill and drain gas and water vessels.",
                }
            },
            {
                Craftable.IceDrill, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.Steel),
                        new ResourceVolumeEntry(.25f, Matter.Copper)
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

        public static Dictionary<Matter, PrinterData> PrinterData = new Dictionary<Matter, PrinterData>()
        {
            {
                Matter.CopperWire, new PrinterData()
                {
                    BuildTimeHours = 1,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.CopperPowder)
                    },
                    Description = "Copper wires.",
                }
            },
            {
                Matter.IronSheeting, new PrinterData()
                {
                    BuildTimeHours = 2,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.IronPowder),
                    },
                    Description = "Thin sheets of iron.",
                }
            },
            {
                Matter.IronBeams, new PrinterData()
                {
                    BuildTimeHours = 3,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.IronPowder),
                    },
                    Description = "Solid reinforced structural iron.",
                }
            },
            {
                Matter.ElectricMotor, new PrinterData()
                {
                    BuildTimeHours = 10,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.IronPowder),
                        new ResourceVolumeEntry(.25f, Matter.CopperWire)
                    },
                    Description = "A brushless electric motor.",
                }
            },
            {
                Matter.Piping, new PrinterData()
                {
                    BuildTimeHours = 5,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.IronPowder),
                    },
                    Description = "Pipes and valves for transporting fluids.",
                }
            },
            {
                Matter.PressureCanvas, new PrinterData()
                {
                    BuildTimeHours = 1,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(.25f, Matter.Canvas),
                        new ResourceVolumeEntry(.25f, Matter.Polyethylene),
                    },
                    Description = "Plastic-covered canvas for inflatable habitats.",
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