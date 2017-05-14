using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;
using System.Linq;

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
        Flywheel,
        Airlock
    }

    /// <summary>
    /// Top level groups that organize modules
    /// "None" means no groups should show
    /// "Undecided" means groups can show but no group is selected
    /// TODO: remove None and Undecided into their own booleans ShowingGroups and HasGroupSelected
    /// </summary>
    public enum ConstructionGroup { Undecided = -1, LifeSupport, Power, Extraction, Refinement, Storage, Other }

    public class BuildingData
    {
        public int BuildTime = Construction.DefaultBuildTimeSeconds;
        public List<ResourceEntry> Requirements;
        public int? Power;
        public int? EnergyStorage;
        public Matter StorageType;
        public int? Storage;
        public Dictionary<Matter, int> IO;
        public string Description;
    }

    public static class Construction
    {
        public const int DefaultBuildTimeSeconds = 10;

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
        public static Dictionary<Module, BuildingData> BuildData = new Dictionary<Module, BuildingData>
        {
            {
                Module.SolarPanelSmall, new BuildingData()
                {

                }
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
                    new ResourceEntry(1, Matter.Biomass),
                }
            },
            {
                Module.Warehouse, new List<ResourceEntry>()
                {
                    new ResourceEntry(2, Matter.Steel),
                    new ResourceEntry(2, Matter.Canvas)
                }
            },
            {
                Module.JunctionBox, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Copper)
                }
            },
            {
                Module.Airlock, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel)
                }
            },
            {
                Module.Workshop, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel),
                    new ResourceEntry(1, Matter.Canvas)
                }
            },
            {
                Module.HallwayNode, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel),
                    new ResourceEntry(1, Matter.Glass)
                }
            },
            {
                Module.GreenhouseHall, new List<ResourceEntry>()
                {
                    new ResourceEntry(1, Matter.Steel),
                    new ResourceEntry(1, Matter.Glass)
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
                    Module.Airlock,
                    Module.HallwayNode,
                    Module.Workshop,
                    Module.GreenhouseHall,
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
                    Module.Splitter,
                    Module.SmallGasTank,
                    Module.LargeGasTank,
                    Module.SmallWaterTank,
                    Module.Warehouse
                }
            },
        };

        public enum CorridorVertexGroup { OriginInner, OriginOuter, NorthInner, NorthOuter, SouthInner, SouthOuter }
        public static readonly Dictionary<CorridorVertexGroup, int[]> CorridorVertexProgressions = new Dictionary<CorridorVertexGroup, int[]>
        {
            //original guide rings
            //{ CorridorVertexGroup.OriginInner, new int[] {  0,  1,  2,  3,  5,  4,  7,  6 } },
            //{ CorridorVertexGroup.OriginOuter, new int[] { 12, 13, 15, 14, 10, 11,  8,  9 } },
            //reversed guide rings
            { CorridorVertexGroup.OriginInner, new int[] {  0,  1,  2,  3,  5,  4,  7,  6 }.Reverse().ToArray() },
            { CorridorVertexGroup.OriginOuter, new int[] { 12, 13, 15, 14, 10, 11,  8,  9 }.Reverse().ToArray() },
            //we used to go by index through both the guide rings and the corridor mesh
            //but Unity messes up the corridor mesh vertices
            //double mesh (3 sets of rings, north, origin, south)
            //{ CorridorVertexGroup.NorthInner,  new int[] {  9, 13,  8, 12, 15, 14, 17, 16 } },
            //{ CorridorVertexGroup.NorthOuter,  new int[] { 35, 34, 32, 33, 37, 36, 39, 38 } },
            //{ CorridorVertexGroup.SouthInner,  new int[] { 20, 10, 18, 19, 11, 21, 23, 22 } },
            //{ CorridorVertexGroup.SouthOuter,  new int[] { 43, 42, 40, 41, 45, 44, 47, 46 } }
            //simple mesh (2 sets of rings, north and south)
            //{ CorridorVertexGroup.NorthInner,  new int[] {  1,  5,  0,  4,  7,  6,  9,  8 } },
            //{ CorridorVertexGroup.NorthOuter,  new int[] { 19, 18, 16, 17, 21, 20, 23, 22 } },
            //{ CorridorVertexGroup.SouthInner,  new int[] { 12,  2, 10, 11,  3, 13, 15, 14 } },
            //{ CorridorVertexGroup.SouthOuter,  new int[] { 27, 26, 24, 25, 29, 28, 31, 30 } }
        };

        //we used to go by index through both the guide rings and the corridor mesh
        //but Unity messes up the corridor mesh vertices
        //private static void SetCorridorVertices(Transform corridorT, Vector3[] corridorVerts, CorridorVertexGroup corridorG, Transform anchorT, Vector3[] anchorVerts, CorridorVertexGroup anchorG)
        //{
        //    int[] corridorI = CorridorVertexProgressions[corridorG];
        //    int[] anchorI = CorridorVertexProgressions[anchorG];

        //    corridorVerts[corridorI[0]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[0]]));
        //    corridorVerts[corridorI[1]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[1]]));
        //    corridorVerts[corridorI[2]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[2]]));
        //    corridorVerts[corridorI[3]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[3]]));
        //    corridorVerts[corridorI[4]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[4]]));
        //    corridorVerts[corridorI[5]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[5]]));
        //    corridorVerts[corridorI[6]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[6]]));
        //    corridorVerts[corridorI[7]] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[anchorI[7]]));
        //}

        public static void SetCorridorVertices(Transform corridorT, Mesh corridorM, Transform anchorT1, Mesh anchorM1, Transform anchorT2, Mesh anchorM2)
        {
            Vector3[] corridorVerts = corridorM.vertices;
            Vector3[] anchorVerts = anchorM1.vertices;

            int anchorI;
            CorridorVertexGroup anchorRing = CorridorVertexGroup.OriginOuter;
            Transform anchorT = null;

            //int[] counts = new int[5];
            for (int corridorI = 0; corridorI < corridorVerts.Length; corridorI++)
            {
                //the "ring" is _encoded_ in the X value of the verts that need to programmatically change
                int ringFromVertexX = (int)corridorVerts[corridorI].x;
                switch (ringFromVertexX)
                {
                    //a vertex with an X of 0 _will not have its position changed_ as it is the center of the corridor, not an end
                    //otherwise, each int corresponds to a ring (outer/inner) and an end (north/south or 1/2)
                    case -2: //rear outer
                        anchorRing = CorridorVertexGroup.OriginOuter;
                        anchorT = anchorT2;
                        goto case 99;
                    case -1: //rear inner
                        anchorRing = CorridorVertexGroup.OriginInner;
                        anchorT = anchorT2;
                        goto case 99;
                    case 1: //front inner
                        anchorRing = CorridorVertexGroup.OriginInner;
                        anchorT = anchorT1;
                        goto case 99;
                    case 2: //front outer
                        anchorRing = CorridorVertexGroup.OriginOuter;
                        anchorT = anchorT1;
                        goto case 99;
                    //no verts should have an X of 99, we use this label for control flow to avoid a function call
                    case 99:
                        //yech we have to round
                        anchorI = Mathf.RoundToInt(corridorVerts[corridorI].y * 10f);
                        // here's a giant debug call for when we had bugs
                        //    UnityEngine.Debug.Log(string.Format(
                        //        "raw Y {5}, anchor I {6}, Anchor group {0}, progression index {1}, anchor vert {2}, global pos {3}, local Corr pos {4}", 
                        //        anchorGroup.ToString(), 
                        //        CorridorVertexProgressions[anchorGroup][anchorI],
                        //        anchorVerts[CorridorVertexProgressions[anchorGroup][anchorI]],
                        //        anchorT.TransformPoint(anchorVerts[CorridorVertexProgressions[anchorGroup][anchorI]]),
                        //        corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[CorridorVertexProgressions[anchorGroup][anchorI]])),
                        //        corridorVerts[corridorI].y,
                        //        anchorI
                        //     ));
                        //set the vertex at corridorI
                        //to the local position
                        //of the anchor's vertex in global position
                        //given the anchor index encoded in the corridor vertex's Y value
                        //and a given anchor group (outer or inner ring)
                        corridorVerts[corridorI] = corridorT.InverseTransformPoint(anchorT.TransformPoint(anchorVerts[CorridorVertexProgressions[anchorRing][anchorI]]));
                        break;
                }
            }
            //UnityEngine.Debug.Log(string.Format("{0}, {1}, {2}, {3}, {4}", counts[0], counts[1], counts[2], counts[3], counts[4]));

            corridorM.vertices = corridorVerts;
            corridorM.RecalculateNormals();
        }
    }

    public interface IBuildable
    {
        void InitializeStartingData();
    }

    public interface IHarvestable
    {
        bool CanHarvest { get; }
        void Harvest();
    }
}
