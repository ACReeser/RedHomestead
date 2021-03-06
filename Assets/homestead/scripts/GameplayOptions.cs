﻿using RedHomestead.Crafting;
using RedHomestead.Economy;
using RedHomestead.Geography;
using RedHomestead.Perks;
using RedHomestead.Simulation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RedHomestead.Buildings;

namespace RedHomestead.GameplayOptions
{
    public class BoughtMatter: Dictionary<Matter, int>
    {
        public void Set(Matter m, int input)
        {
            this[m] = input;
        }
    }


    public class NewGameChoices
    {
        public Perk ChosenPlayerTraining;
        public string HomesteadName, PlayerName;
        public BackerFinancing ChosenFinancing;
        public BaseLocation ChosenLocation;
        public int StartingFunds, AllocatedFunds, RemainingFunds;
        public bool BuyRover, IsTutorial;
        public BoughtMatter BoughtMatter;
        public Dictionary<Craftable, int> BoughtCraftables;

        public void Init()
        {
            PlayerName = "Everyman";
            BoughtMatter = new BoughtMatter();
            BoughtCraftables = new Dictionary<Craftable, int>();
            ChosenFinancing = BackerFinancing.Government;
            ChosenPlayerTraining = Perk.Athlete;
        }

        public void RecalculateFunds()
        {
            StartingFunds = Mathf.RoundToInt(ChosenFinancing.Data().StartingFunds * PerkMultipliers.StartingFunds(ChosenPlayerTraining));
            AllocatedFunds = EconomyExtensions.HabitatCost + EconomyExtensions.RoverCost;

            int i = 0;
            foreach(KeyValuePair<Matter, int> kvp in BoughtMatter)
            {
                AllocatedFunds += EconomyExtensions.StartingSupplies[kvp.Key].PerUnitCost * kvp.Value;
                i++;
            }
            i = 0;
            foreach (KeyValuePair<Craftable, int> kvp in BoughtCraftables)
            {
                AllocatedFunds += EconomyExtensions.StartingCraftables[kvp.Key].PerUnitCost * kvp.Value;
                i++;
            }

            RemainingFunds = StartingFunds - AllocatedFunds;
        }

        public float[] GetPerkProgress()
        {
            int[] perks = Enum.GetValues(typeof(Perk)).Cast<int>().ToArray();
            float[] result = new float[perks.Length];
            for (int i = 0; i < perks.Length; i++)
            {
                if ((Perk)i == ChosenPlayerTraining)
                {
                    result[i] = 2f;
                }
                else
                {
                    result[i] = 0f;
                }
            }

            return result;
        }

        internal void AddMinimumSupplies()
        {
            if (BoughtMatter != null)
            {
                AddSuppliesFromModule(Module.Airlock);
                AddSuppliesFromModule(Module.EVAStation);
                AddSuppliesFromModule(Module.RoverStation);
            }
        }

        private void AddSuppliesFromModule(Module module)
        {
            foreach(IResourceEntry entry in Construction.BuildData[module].Requirements)
            {
                AddOrIncrement(BoughtMatter, entry.Type, Mathf.CeilToInt(entry.AmountByVolume));
            }
        }

        private void AddOrIncrementVolume(Dictionary<Matter, int> dictionary, Matter key, float addition)
        {
            AddOrIncrement<Matter>(dictionary, key, Mathf.CeilToInt(addition * key.CubicMetersPerUnit()));
        }

        private void AddOrIncrement<K>(Dictionary<K, int> dictionary, K key, int additionUnits)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] += additionUnits;
            }
            else
            {
                dictionary[key] = additionUnits;
            }
        }

        internal void AddBackerSupplies()
        {
            if (BoughtMatter != null)
            {
                switch (ChosenFinancing)
                {
                    default:
                    case BackerFinancing.Government:
                        AddOrIncrement(BoughtMatter, Matter.RationMeals, 7 * 3);
                        break;
                    case BackerFinancing.TechCorp:
                        AddOrIncrement(BoughtMatter, Matter.SolarPanels, 4);
                        break;
                    case BackerFinancing.IndustryCorp:
                        AddOrIncrement(BoughtMatter, Matter.IronPowder, 4);
                        break;
                    case BackerFinancing.Benefactor:
                        AddSuppliesFromModule(Module.SmallGasTank);
                        AddOrIncrement(BoughtMatter, Matter.Hydrogen, 10);
                        break;
                }
            }
        }

        internal void LoadQuickstart()
        {
            this.Init();
            BoughtMatter.Set(Matter.RationMeals, 1);
            ChosenLocation = new BaseLocation()
            {
                Region = MarsRegion.meridiani_planum
            };
            AddOrIncrementVolume(BoughtMatter, Matter.Water, 2);
            AddOrIncrementVolume(BoughtMatter, Matter.Oxygen, 2);
            this.AddSuppliesFromModule(Module.SmallGasTank);
            this.AddSuppliesFromModule(Module.SabatierReactor);

            this.RecalculateFunds();
        }
    }
}