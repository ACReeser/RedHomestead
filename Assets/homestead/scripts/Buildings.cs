using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;

namespace RedHomestead.Buildings
{
    public enum Module
    {
        Unspecified = -1,
        SolarPanelSmall,
        SabatierReactor,
        SmallGasTank,
        LargeGasTank,
        Warehouse,
        Habitat,
        OreExtractor,
        Smelter,
        SmallWaterTank,
        Splitter,
        WaterElectrolyzer,
        AlgaeTank,
        RTG,
        GreenhouseHall,
        JunctionBox,
        Workshop,
        HallwayNode,
        Flywheel
    }

    /// <summary>
    /// Top level groups that organize modules
    /// "None" means no groups should show
    /// "Undecided" means groups can show but no group is selected
    /// TODO: remove None and Undecided into their own booleans ShowingGroups and HasGroupSelected
    /// </summary>
    public enum ConstructionGroup { Undecided = -1, LifeSupport, Power, Extraction, Refinement, Storage, Other }

    public static class Construction
    {
        private const int DefaultBuildTimeSeconds = 10;

        /// <summary>
        /// In seconds
        /// </summary>
        public static Dictionary<Module, int> BuildTimes = new Dictionary<Module, int>
        {
            {
                Module.SolarPanelSmall, DefaultBuildTimeSeconds
            },
            {
                Module.LargeGasTank, DefaultBuildTimeSeconds
            },
            {
                Module.SmallGasTank, DefaultBuildTimeSeconds
            },
            {
                Module.SmallWaterTank, DefaultBuildTimeSeconds
            },
            {
                Module.Splitter, DefaultBuildTimeSeconds
            },
            {
                Module.SabatierReactor, DefaultBuildTimeSeconds
            },
            {
                Module.OreExtractor, DefaultBuildTimeSeconds
            },
            {
                Module.WaterElectrolyzer, DefaultBuildTimeSeconds
            },
            {
                Module.AlgaeTank, DefaultBuildTimeSeconds
            },
            {
                Module.Warehouse, DefaultBuildTimeSeconds
            }
        };

        //todo: load from file probably
        //todo: disallow duplicate resource types by using another dict instead of a list
        public static Dictionary<Module, List<ResourceEntry>> Requirements = new Dictionary<Module, List<ResourceEntry>>()
        {
            {
                Module.SolarPanelSmall, new List<ResourceEntry>()
                {
                    new ResourceEntry(2, Matter.Steel),
                    new ResourceEntry(4, Matter.SiliconWafers)
                }
            },
            {
                Module.LargeGasTank, new List<ResourceEntry>()
                {
                    new ResourceEntry(8, Matter.Steel)
                }
            },
            {
                Module.SmallGasTank, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.SmallWaterTank, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.Splitter, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.SabatierReactor, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.OreExtractor, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.WaterElectrolyzer, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.AlgaeTank, new List<ResourceEntry>()
                {
                    new ResourceEntry(2, Matter.Steel),
                    new ResourceEntry(2, Matter.Glass),
                    new ResourceEntry(2, Matter.Biomass),
                }
            },
            {
                Module.Warehouse, new List<ResourceEntry>()
                {
                    new ResourceEntry(2, Matter.Steel),
                    new ResourceEntry(4, Matter.Canvas)
                }
            },
            {
                Module.JunctionBox, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Copper)
                }
            }
        };


        /// <summary>
        /// from group to list of modules
        /// </summary>
        public static Dictionary<ConstructionGroup, Module[]> ConstructionGroupmap = new Dictionary<ConstructionGroup, Module[]>()
        {
            {
                ConstructionGroup.LifeSupport,
                new Module[]
                {
                    //Module.Habitat,
                    //Module.Workspace
                }
            },
            {
                ConstructionGroup.Other,
                new Module[]
                {
                    //Module.Habitat,
                    //Module.Workspace
                }
            },
            {
                ConstructionGroup.Power,
                new Module[]
                {
                    Module.SolarPanelSmall,
                    Module.JunctionBox
                }
            },
            {
                ConstructionGroup.Extraction,
                new Module[]
                {
                    Module.SabatierReactor,
                    Module.AlgaeTank
                    //Module.OreExtractor,
                }
            },
            {
                ConstructionGroup.Refinement,
                new Module[]
                {
                    Module.WaterElectrolyzer
                }
            },
            {
                ConstructionGroup.Storage,
                new Module[]
                {
                    //Module.Splitter,
                    Module.SmallGasTank,
                    //Module.LargeGasTank,
                    Module.SmallWaterTank,
                    Module.Warehouse
                }
            },
        };
    }

    public interface IHarvestable
    {
        bool CanHarvest { get; }
        void Harvest();
    }
}
