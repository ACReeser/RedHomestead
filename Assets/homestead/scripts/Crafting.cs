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
        Toolbox,
        EVAOxygenTank,
        EVABatteries,
        EVAToolbelt,
        EVAJumpjets
    }

    public enum CraftableGroup
    {
        Power, Storage, Extraction, SuitUpgrade
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
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                        new ResourceUnitEntry(1, Matter.CopperWire),
                        new ResourceUnitEntry(1, Matter.SolarPanels)
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
                        new ResourceUnitEntry(1, Matter.CopperWire),
                        new ResourceVolumeEntry(.5f, Matter.Aluminium),
                        new ResourceUnitEntry(1, Matter.Glass)
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
                        new ResourceUnitEntry(1, Matter.IronSheeting)
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
                        new ResourceUnitEntry(1, Matter.ElectricMotor),
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                        new ResourceUnitEntry(1, Matter.Piping)
                    },
                    Description = "A portable pump to fill and drain gas and water vessels.",
                }
            },
            {
                Craftable.IceDrill, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                        new ResourceUnitEntry(1, Matter.ElectricMotor),
                        new ResourceUnitEntry(1, Matter.CopperWire)
                    },
                    Description = "A portable drill to mine water ice from deposits.",
                    PowerSteady = 1
                }
            },
            {
                Craftable.EVABatteries, new CraftingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.CopperWire),
                        new ResourceVolumeEntry(.25f, Matter.Aluminium),
                        new ResourceUnitEntry(1, Matter.Glass)
                    },
                    Description = "Extra batteries for the EVA Suit."
                }
            },
            {
                Craftable.EVAOxygenTank, new CraftingData()
                {
                    BuildTimeHours = 15,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.ElectricMotor),
                        new ResourceVolumeEntry(.25f, Matter.Steel),
                        new ResourceUnitEntry(1, Matter.Piping)
                    },
                    Description = "Extra oxygen for the EVA Suit."
                }
            },
            {
                Craftable.EVAJumpjets, new CraftingData()
                {
                    BuildTimeHours = 15,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(2, Matter.ElectricMotor),
                        new ResourceVolumeEntry(.5f, Matter.Aluminium),
                        new ResourceUnitEntry(1, Matter.Piping)
                    },
                    Description = "Extra oxygen for the EVA Suit."
                }
            },
            {
                Craftable.EVAToolbelt, new CraftingData()
                {
                    BuildTimeHours = 5,
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.Canvas),
                        new ResourceVolumeEntry(.25f, Matter.Aluminium),
                    },
                    Description = "Extra tool slots for the EVA Suit."
                }
            }
        };

        public static bool IsCraftableEVASuitComponent(this Craftable craftable)
        {
            switch (craftable)
            {
                case Craftable.EVABatteries:
                case Craftable.EVAOxygenTank:
                case Craftable.EVAToolbelt:
                case Craftable.EVAJumpjets:
                    return true;
                default:
                    return false;
            }
        }

        public static EVA.EVAUpgrade ToEVASuitUpgrade(this Craftable craftable)
        {
            switch (craftable)
            {
                case Craftable.EVABatteries:
                    return EVA.EVAUpgrade.Battery;
                case Craftable.EVAOxygenTank:
                    return EVA.EVAUpgrade.Oxygen;
                case Craftable.EVAToolbelt:
                    return EVA.EVAUpgrade.Toolbelt;
                case Craftable.EVAJumpjets:
                    return EVA.EVAUpgrade.Jetpack;
                default:
                    return EVA.EVAUpgrade.None;
            }
        }
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
            {
                CraftableGroup.SuitUpgrade,
                new Craftable[]
                {
                    Craftable.EVAToolbelt,
                    Craftable.EVAOxygenTank,
                    Craftable.EVABatteries,
                    Craftable.EVAJumpjets,
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