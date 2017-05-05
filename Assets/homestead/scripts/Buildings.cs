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

        public static int BuildRadius(Module module)
        {
            switch (module)
            {
                case Module.JunctionBox:
                    return 1;
                case Module.Flywheel:
                case Module.Splitter:
                    return 2;
                case Module.RTG:
                case Module.SabatierReactor:
                case Module.AlgaeTank:
                case Module.SmallWaterTank:
                    return 3;
                case Module.GreenhouseHall:
                    return 4;
                case Module.Warehouse:
                    return 6;
                default:
                    return 5;
            }
        }

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

        public enum CorridorVertexGroup { OriginInner, OriginOuter, NorthInner, NorthOuter, SouthInner, SouthOuter }
        public static readonly Dictionary<CorridorVertexGroup, int[]> CorridorVertexProgressions = new Dictionary<CorridorVertexGroup, int[]>
        {
            { CorridorVertexGroup.OriginInner, new int[] {  0,  1,  2,  3,  5,  4,  7,  6 } },
            { CorridorVertexGroup.OriginOuter, new int[] { 12, 13, 15, 14, 10, 11,  8,  9 } },
            { CorridorVertexGroup.NorthInner,  new int[] {  9, 13,  8, 12, 15, 14, 17, 16 } },
            { CorridorVertexGroup.NorthOuter,  new int[] { 35, 34, 32, 33, 37, 36, 39, 38 } },
            { CorridorVertexGroup.SouthInner,  new int[] { 20, 10, 18, 19, 11, 21, 23, 22 } },
            { CorridorVertexGroup.SouthOuter,  new int[] { 43, 42, 40, 41, 45, 44, 47, 46 } }
        };

        private static void SetCorridorVertices(Transform corridorT, Vector3[] corridorVerts, CorridorVertexGroup corridorG, Transform anchorT, Vector3[] anchorVerts, CorridorVertexGroup anchorG)
        {
            int[] corridorI = CorridorVertexProgressions[corridorG];
            int[] anchorI = CorridorVertexProgressions[anchorG];

            corridorVerts[corridorI[0]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[0]]));
            corridorVerts[corridorI[1]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[1]]));
            corridorVerts[corridorI[2]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[2]]));
            corridorVerts[corridorI[3]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[3]]));
            corridorVerts[corridorI[4]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[4]]));
            corridorVerts[corridorI[5]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[5]]));
            corridorVerts[corridorI[6]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[6]]));
            corridorVerts[corridorI[7]] = corridorT.InverseTransformVector(anchorT.TransformVector(anchorVerts[anchorI[7]]));
        }

        public static void SetCorridorVertices(Transform corridorT, Mesh corridorM, Transform anchorT1, Mesh anchorM1, Transform anchorT2, Mesh anchorM2)
        {
            Vector3[] corridorVerts = corridorM.vertices;
            Vector3[] anchorVerts = anchorM1.vertices;

            SetCorridorVertices(corridorT, corridorVerts, CorridorVertexGroup.NorthInner, anchorT1, anchorVerts, CorridorVertexGroup.OriginInner);
            SetCorridorVertices(corridorT, corridorVerts, CorridorVertexGroup.NorthOuter, anchorT1, anchorVerts, CorridorVertexGroup.OriginOuter);
            SetCorridorVertices(corridorT, corridorVerts, CorridorVertexGroup.SouthInner, anchorT2, anchorVerts, CorridorVertexGroup.OriginInner);
            SetCorridorVertices(corridorT, corridorVerts, CorridorVertexGroup.SouthOuter, anchorT2, anchorVerts, CorridorVertexGroup.OriginOuter);

            corridorM.vertices = corridorVerts;
        }
    }

    public interface IHarvestable
    {
        bool CanHarvest { get; }
        void Harvest();
    }
}
