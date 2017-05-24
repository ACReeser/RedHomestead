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

        };

        public static Sprite AtlasSprite(this Craftable craft)
        {
            return IconAtlas.Instance.CraftableIcons[Convert.ToInt32(craft)];
        }
    }
}