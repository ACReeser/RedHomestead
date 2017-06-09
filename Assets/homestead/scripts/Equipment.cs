using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Equipment
{
    public enum Equipment { Locked = -1, EmptyHand = 0, Drill, Blueprints, ChemicalSniffer, Wheelbarrow, Scanner, Wrench, Sidearm, LMG, Screwdriver }
    public enum Slot { Unequipped = 4, PrimaryTool = 5, SecondaryTool = 3, PrimaryGadget = 1, SecondaryGadget = 0, TertiaryGadget = 2 }

    public class Loadout
    {
        private Equipment[] OutdoorGadgets = new Equipment[] { Equipment.Blueprints, Equipment.ChemicalSniffer, Equipment.Locked };
        private Equipment[] IndoorGadgets = new Equipment[] { Equipment.Screwdriver, Equipment.Wheelbarrow, Equipment.Locked };

        private Dictionary<Slot, Equipment> _loadout = new Dictionary<Slot, Equipment>()
        {
            { Slot.Unequipped, Equipment.EmptyHand },
            { Slot.PrimaryTool, Equipment.Drill },
            { Slot.SecondaryTool, Equipment.Locked },
            { Slot.PrimaryGadget, Equipment.Blueprints },
            { Slot.SecondaryGadget, Equipment.ChemicalSniffer },
            { Slot.TertiaryGadget, Equipment.Locked },
        };

        public Equipment this[Slot s]
        {
            get
            {
                return _loadout[s];
            }
        }

        public Slot ActiveSlot { get; set; }
        public Equipment Equipped
        {
            get
            {
                return this[this.ActiveSlot];
            }
        }

        public Loadout()
        {
            this.ActiveSlot = Slot.Unequipped;
        }

        public bool IsConstructingExterior
        {
            get
            {
                return Equipped == Equipment.Blueprints && SurvivalTimer.Instance.UsingPackResources;
            }
        }

        public bool IsConstructingInterior
        {
            get
            {
                return Equipped == Equipment.Blueprints && SurvivalTimer.Instance.IsInHabitat;
            }
        }

        public void RefreshGadgetsBasedOnLocation()
        {
            if (SurvivalTimer.Instance.IsInHabitat)
            {
                _loadout[Slot.PrimaryGadget] = IndoorGadgets[0];
                _loadout[Slot.SecondaryGadget] = IndoorGadgets[1];
                _loadout[Slot.TertiaryGadget] = IndoorGadgets[2];
            }
            else
            {
                _loadout[Slot.PrimaryGadget] = OutdoorGadgets[0];
                _loadout[Slot.SecondaryGadget] = OutdoorGadgets[1];
                _loadout[Slot.TertiaryGadget] = OutdoorGadgets[2];
            }

            GuiBridge.Instance.BuildRadialMenu(this);
        }

        internal void PutEquipmentInSlot(Slot aSlot, Equipment e)
        {
            this._loadout[aSlot] = e;
        }

        private Equipment preWrenchPrimarySlot;
        internal void ToggleRepairWrench(bool doRepairs)
        {
            if (doRepairs)
            {
                preWrenchPrimarySlot = this[Slot.PrimaryTool];
                PutEquipmentInSlot(Slot.PrimaryTool, Equipment.Wrench);
            }
            else
            {
                PutEquipmentInSlot(Slot.PrimaryTool, preWrenchPrimarySlot);
            }
        }
    }

    [Serializable]
    public struct EquipmentSprites
    {
        public Sprite[] Sprites;
        public Sprite Locked;

        internal Sprite FromEquipment(Equipment e)
        {
            if (e == Equipment.Locked)
                return Locked;

            return Sprites[(int)e];
        }
    }
}