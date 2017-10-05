using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Simulation;

namespace RedHomestead.Interiors
{

    public enum StuffGroup { Bedroom, Kitchen, Storage, Workspace, Lights }

    public enum Stuff {
        //bedroom
        Bed, Desk, Terminal, Couch,
        //kitchen
        Table, Kitchen, Pantry,
        //storage
        OxygenStorage, WaterStorage, BatteryBank, Shelving,
        //workspace
        Workbench, Hydroponics, ThreeDPrinter,
        //lights
        FloorLight, Skylight
    }

    public struct StuffInformation{
        public string Description { get; set; }
        public List<ResourceVolumeEntry> Requirements { get; set; }
    }


    /// <summary>
    /// Top level groups that organize floorplans
    /// </summary>
    public enum FloorplanGroup { Undecided = -1, Floor, Edge, Corner }

    /// <summary>
    /// Second level groups that organize floorplans
    /// </summary>
    public enum Floorplan { SolidFloor, MeshFloor, SolidWall, MeshWall, Door, Window, Column, SingleColumnWall, DoubleColumnWall }

    public enum FloorplanMaterial { Concrete, Brick, Metal, Plastic, Rock, Glass }

    public static class InteriorMap
    {
        public static Dictionary<StuffGroup, Stuff[]> StuffGroups = new Dictionary<StuffGroup, Stuff[]>()
        {
            {
                StuffGroup.Bedroom,
                new Stuff[]
                {
                    Stuff.Bed,
                    Stuff.Desk,
                    Stuff.Couch,
                    Stuff.Terminal
                }
            },
            {
                StuffGroup.Kitchen,
                new Stuff[]
                {
                    Stuff.Table,
                    Stuff.Kitchen,
                    Stuff.Pantry,
                }
            },
            {
                StuffGroup.Storage,
                new Stuff[]
                {
                    Stuff.OxygenStorage,
                    Stuff.WaterStorage,
                    Stuff.BatteryBank,
                    Stuff.Shelving
                }
            },
            {
                StuffGroup.Workspace,
                new Stuff[]
                {
                    Stuff.Workbench,
                    Stuff.Hydroponics,
                    Stuff.ThreeDPrinter
                }
            },
            {
                StuffGroup.Lights,
                new Stuff[]
                {
                    Stuff.FloorLight,
                    Stuff.Skylight
                }
            }
        };

        public static Dictionary<Stuff, StuffInformation> StuffInformation = new Dictionary<Stuff, Interiors.StuffInformation>()
        {
            {
                Stuff.Bed,
                new Interiors.StuffInformation()
                {
                    Description = "A bed to sleep in.",
                    Requirements = null
                }
            },
            {
                Stuff.Table,
                new Interiors.StuffInformation()
                {
                    Description = "A table to eat on.",
                    Requirements = null
                }
            }
        };


        public static Dictionary<FloorplanGroup, Floorplan[]> FloorplanGroupmap = new Dictionary<FloorplanGroup, Floorplan[]>()
        {
            {
                FloorplanGroup.Floor,
                new Floorplan[]
                {
                    Floorplan.SolidFloor,
                    Floorplan.MeshFloor
                }
            },
            {
                FloorplanGroup.Edge,
                new Floorplan[]
                {
                    Floorplan.SolidWall,
                    Floorplan.MeshWall,
                    Floorplan.Window,
                    Floorplan.Door,
                    Floorplan.SingleColumnWall,
                    Floorplan.DoubleColumnWall
                }
            },
            {
                FloorplanGroup.Corner,
                new Floorplan[]
                {
                    Floorplan.Column
                }
            }
        };
    }
}