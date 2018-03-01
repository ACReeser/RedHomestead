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
        Furnace,
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
        Airlock,
        EVAStation,
        RoverStation,
        WeatherStation,
        GlassFurnace,
        ThreeDPrinter,
        AirMiner,
        GroundSolarPanel,
        Market,
        ScienceLab
    }

    /// <summary>
    /// Top level groups that organize modules
    /// "None" means no groups should show
    /// "Undecided" means groups can show but no group is selected
    /// </summary>
    public enum ConstructionGroup { Undecided = -1, LifeSupport, Power, Extraction, Refinement, Storage, Other }

    public class BuildingData : Crafting.BlueprintData
    {
        public BuildingData()
        {
            this.BuildTimeHours = Construction.DefaultBuildTimeHours;
        }
    }

    public static class Construction
    {
        public const int DefaultBuildTimeHours = 1;

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
                case Module.Market:
                    return 6;
                default:
                    return 5;
            }
        }

        public static bool CanRotate(this Module module)
        {
            switch (module)
            {
                //case Module.GroundSolarPanel:
                case Module.SolarPanelSmall:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsHabitatModule(this Module module)
        {
            switch (module)
            {
                case Module.Workshop:
                case Module.Airlock:
                case Module.GreenhouseHall:
                case Module.HallwayNode:
                case Module.ScienceLab:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// In seconds
        /// </summary>
        public static Dictionary<Module, BuildingData> BuildData = new Dictionary<Module, BuildingData>
        {
            //power
            {
                Module.JunctionBox, new BuildingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.CopperWire)
                    },
                    Description = "A small box to split powerlines."
                }
            },
            {
                Module.GroundSolarPanel, new BuildingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                        new ResourceUnitEntry(2, Matter.SolarPanels)
                    },
                    Description = "A solar panel rack that generates free energy, but only when the sun is shining and the sky is clear.",
                    PowerMin = 0,
                    PowerMax = 9
                }
            },
            {
                Module.SolarPanelSmall, new BuildingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.ElectricMotor),
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                        new ResourceUnitEntry(2, Matter.SolarPanels)
                    },
                    Description = "A solar panel rack that generates free energy, but only when the sun is shining and the sky is clear. The heliotropic function means that the panels always face the sun.",
                    PowerMin = 0,
                    PowerMax = 9
                }
            },
            {
                Module.Flywheel, new BuildingData()
                {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(2, Matter.IronSheeting),
                        new ResourceVolumeEntry(2, Matter.CopperWire)
                    },
                    Description = "A spinning cylinder that stores rotational energy.",
                    EnergyStorage = 10
                }
            },
            //storage
            { 
                Module.Warehouse, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(2, Matter.IronBeams),
                        new ResourceUnitEntry(2, Matter.Canvas)
                    },
                    Description = "A canvas storage area to stash materials.",
                    Storage = 36
                }
            },
            {
                Module.SmallGasTank, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceVolumeEntry(1, Matter.Piping),
                    },
                    Description = "A small pressure vessel to store gasses.",
                    Storage = 10,
                    StorageType = Matter.Methane
                }
            },
            {
                Module.LargeGasTank, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceVolumeEntry(1, Matter.Piping),
                    },
                    Description = "A large pressure vessel to store gasses.",
                    Storage = 50,
                    StorageType = Matter.Methane
                }
            },
            {
                Module.SmallWaterTank, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceVolumeEntry(1, Matter.Piping),
                    },
                    Description = "A small vessel to store water.",
                    Storage = 10,
                    StorageType = Matter.Water
                }
            },
            {
                Module.Splitter, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.Steel),
                    },
                    Description = "A fluid splitter to split pipelines."
                }
            },
            {
                Module.Market, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(2, Matter.IronBeams),
                        new ResourceUnitEntry(2, Matter.Canvas),
                        new ResourceUnitEntry(1, Matter.ElectricMotor),
                    },
                    Description = "A storage area to sell materials.",
                    Storage = 12
                }
            },
            //extraction
            {
                Module.SabatierReactor, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceVolumeEntry(1, Matter.Piping),
                    },
                    Description = "An atmosphere miner that uses hydrogen to create water and methane.",
                    PowerSteady = -3,
                    IO = new Dictionary<Matter, float>()
                    {
                        { Matter.Hydrogen, -3f },
                        { Matter.Water, 1f },
                        { Matter.Methane, 1f }
                    }
                }
            },
            {
                Module.OreExtractor, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.ElectricMotor),
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                    },
                    Description = "An ore miner that scoops up regolith.",
                    PowerSteady = -3,
                    //IO = new Dictionary<Matter, float>()
                    //{
                    //    { Matter.Hydrogen, -3f },
                    //    { Matter.Water, 1f },
                    //    { Matter.Methane, 1f }
                    //}
                }
            },
            //refining
            {
                Module.WaterElectrolyzer, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceVolumeEntry(2, Matter.Polyethylene),
                        new ResourceVolumeEntry(.5f, Matter.CopperWire),
                    },
                    Description = "An tank that converts water to oxygen and hydrogen.",
                    PowerSteady = -3,
                    IO = new Dictionary<Matter, float>()
                    {
                        { Matter.Water, -3f },
                        { Matter.Oxygen, 2f },
                        { Matter.Hydrogen, 1f }
                    }
                }
            },
            {
                Module.AlgaeTank, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceVolumeEntry(1, Matter.Glass),
                        new ResourceVolumeEntry(.5f, Matter.Biomass),
                    },
                    Description = "A transparent tower for growing edible algae.",
                    PowerSteady = -1,
                    IO = new Dictionary<Matter, float>()
                    {
                        { Matter.Water, -1f },
                        { Matter.Biomass, 1f },
                        { Matter.Oxygen, .5f }
                    }
                }
            },
            //hab/life support
            {
                Module.Airlock, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronSheeting),
                        new ResourceUnitEntry(2, Matter.Piping)
                    },
                    Description = "A door for entering and exiting a pressurized habitat."
                }
            },
            {
                Module.GreenhouseHall, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceVolumeEntry(1, Matter.IronBeams),
                        new ResourceVolumeEntry(2, Matter.Polyethylene),
                        new ResourceUnitEntry(1, Matter.Biomass),
                    },
                    Description = "A transparent hallway for growing crops.",
                    PowerSteady = -2,
                    IO = new Dictionary<Matter, float>()
                    {
                        { Matter.Water, -1f },
                        { Matter.Biomass, 1f },
                        { Matter.Oxygen, .5f }
                    }
                }
            },
            {
                Module.Workshop, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(3, Matter.IronSheeting),
                        new ResourceUnitEntry(1, Matter.PressureCanvas),
                    },
                    Description = "A large workspace for crafting and EVA suit upgrades."
                }
            },
            {
                Module.HallwayNode, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(2, Matter.IronSheeting),
                        new ResourceUnitEntry(2, Matter.Glass),
                    },
                    Description = "A 4-way node for expanding the habitat."
                }
            },
            {
                Module.EVAStation, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.CopperWire),
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                    },
                    Description = "A way to recharge oxygen and power during an EVA."
                }
            },
            {
                Module.RoverStation, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.CopperWire),
                        new ResourceUnitEntry(1, Matter.Piping),
                    },
                    Description = "A way to transfer oxygen, water, and power to Rovers."
                }
            },
            {
                Module.WeatherStation, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.CopperWire),
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                    },
                    Description = "A station that allows weather forecasts."
                }
            },
            {
                Module.Furnace, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(4, Matter.IronBeams),
                        new ResourceUnitEntry(2, Matter.IronSheeting),
                        new ResourceUnitEntry(1, Matter.Piping),
                    },
                    Description = "A smelting furnace for metal ore."
                }
            },
            {
                Module.ThreeDPrinter, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                        //new ResourceEntry(1f, Matter.Laser),
                    },
                    Description = "A printer for advanced components."
                }
            },
            {
                Module.AirMiner, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(1, Matter.ElectricMotor),
                        new ResourceUnitEntry(1, Matter.IronSheeting),
                    },
                    Description = "An air pump that mines carbon dioxide."
                }
            },
            {
                Module.ScienceLab, new BuildingData() {
                    Requirements = new List<IResourceEntry>()
                    {
                        new ResourceUnitEntry(2, Matter.CopperWire),
                        new ResourceUnitEntry(3, Matter.IronSheeting),
                        new ResourceUnitEntry(1, Matter.PressureCanvas),
                    },
                    Description = "A laboratory to conduct experiments."
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
                    Module.GreenhouseHall
                }
            },
            {
                ConstructionGroup.Other,
                new Module[]
                {
                    Module.EVAStation,
                    Module.RoverStation,
                    Module.WeatherStation,
                    Module.ScienceLab
                }
            },
            {
                ConstructionGroup.Power,
                new Module[]
                {
                    Module.GroundSolarPanel,
                    Module.SolarPanelSmall,
                    Module.JunctionBox
                }
            },
            {
                ConstructionGroup.Extraction,
                new Module[]
                {
                    Module.SabatierReactor,
                    Module.AlgaeTank,
                    Module.OreExtractor,
                    Module.AirMiner
                }
            },
            {
                ConstructionGroup.Refinement,
                new Module[]
                {
                    Module.WaterElectrolyzer,
                    Module.Furnace,
                    Module.ThreeDPrinter
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
                    Module.Warehouse,
                    Module.Market
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
}
