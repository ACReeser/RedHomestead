using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Equipment
{
    public enum Equipment { Locked = -1, EmptyHand = 0, PowerDrill, Blueprints, ChemicalSniffer, Wheelbarrow, Scanner, Wrench, Sidearm, LMG, Screwdriver, RockDrill, Sledge, Blower }
    public enum Slot { Unequipped = 4, PrimaryTool = 5, SecondaryTool = 3, PrimaryGadget = 1, SecondaryGadget = 0, TertiaryGadget = 2 }

    public class Loadout
    {
        private Equipment[] OutdoorGadgets = new Equipment[] { Equipment.Blueprints, Equipment.ChemicalSniffer, Equipment.Locked };
        private Equipment[] IndoorGadgets = new Equipment[] {
            //Equipment.Screwdriver,
            //Equipment.Wheelbarrow,
            Equipment.Locked,
            Equipment.Locked,
            Equipment.Locked
        };

        private Dictionary<Slot, Equipment> _loadout = new Dictionary<Slot, Equipment>()
        {
            { Slot.Unequipped, Equipment.EmptyHand },
            { Slot.PrimaryTool, Equipment.EmptyHand },
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
                return Equipped == Equipment.Blueprints && SurvivalTimer.Instance.IsNotInHabitat;
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

        internal SlotHint GetSlotHint(Equipment e)
        {
            return new SlotHint()
            {
                MouseButton = (this[Slot.PrimaryTool] == e) ? 0 : 1
            };
        }
    }

    internal struct SlotHint
    {
        public int MouseButton;
        public string KeyHint
        {
            get
            {
                return MouseButton == 0 ? "LMB" : "RMB";
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

    public interface IEquipmentSwappable
    {
        Transform[] Tools { get; }
        Transform[] Lockers { get; }
        Dictionary<Transform, Equipment> EquipmentLockers { get; }
        Equipment[] LockerEquipment { get; }
    }

    public static class EquipmentExtensions
    {
        public static void InitializeSwappable(this IEquipmentSwappable swapper)
        {
            for (int i = 0; i < swapper.Lockers.Length; i++)
            {
                swapper.EquipmentLockers[swapper.Lockers[i]] = swapper.LockerEquipment[i];
            }
        }

        public static void SwapEquipment(this IEquipmentSwappable swapper, Transform lockerT)
        {
            Equipment fromLocker = swapper.EquipmentLockers[lockerT],
                fromPlayer = PlayerInput.Instance.Loadout[Slot.PrimaryTool];

            int lockerIndex = Array.IndexOf(swapper.Lockers, lockerT);

            PlayerInput.Instance.Loadout.PutEquipmentInSlot(Slot.PrimaryTool, fromLocker);
            swapper.EquipmentLockers[lockerT] = fromPlayer;
            swapper.LockerEquipment[lockerIndex] = fromPlayer;

            int equipmentIndex = Convert.ToInt32(fromPlayer);
            Transform prefab = PlayerInput.Instance.ToolPrefabs[equipmentIndex];

            MeshRenderer meshR = swapper.Tools[lockerIndex].GetComponent<MeshRenderer>();
            if (prefab == null)
            {
                meshR.enabled = false;
            }
            else
            {
                swapper.Tools[lockerIndex].GetComponent<MeshFilter>().mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
                meshR.enabled = true;
                meshR.materials = prefab.GetComponent<MeshRenderer>().sharedMaterials;
            }

            GuiBridge.Instance.BuildRadialMenu(PlayerInput.Instance.Loadout);
            GuiBridge.Instance.RefreshEquipped();
        }

        public static PromptInfo GetLockerPrompt(this IEquipmentSwappable swapper, Transform transform)
        {
            Equipment fromLocker = swapper.EquipmentLockers[transform],
                current = PlayerInput.Instance.Loadout[Slot.PrimaryTool];

            if (fromLocker != current)
            {
                string desc = "";
                if (current == Equipment.EmptyHand)
                    desc = "Pick up " + fromLocker.ToString();
                else if (fromLocker == Equipment.EmptyHand)
                    desc = "Put away " + current.ToString();
                else
                    desc = String.Format("Swap {0} for {1}", current.ToString(), fromLocker.ToString());

                Prompts.SwapEquipmentHint.Description = desc;
                return Prompts.SwapEquipmentHint;
            }
            else
            {
                return null;
            }
        }
    }
}