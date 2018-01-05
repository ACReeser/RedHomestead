using RedHomestead.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Equipment
{
    public enum Equipment { Locked = -1, EmptyHand = 0, PowerDrill, Blueprints, ChemicalSniffer, Wheelbarrow, Wrench, Sidearm, LMG, Screwdriver, RockDrill, Sledge, Blower, Sampler, GPS, Multimeter }
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

        private Dictionary<Slot, Equipment> _loadout = new Dictionary<Slot, Equipment>();

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
            for (int i = 0; i < Game.Current.Player.Loadout.Length; i++)
            {
                _loadout[(Slot)i] = Game.Current.Player.Loadout[i];
            }
            
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

        internal void UpgradeToBigToolbelt()
        {
            this.OutdoorGadgets[2] = Equipment.EmptyHand;
            this._loadout[Slot.SecondaryTool] = Equipment.EmptyHand;
            this._loadout[Slot.TertiaryGadget] = Equipment.EmptyHand;
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

        internal Equipment[] MarshalLoadout()
        {
            var result = new Equipment[6];
            //loop unrolled for performance, also no function call for performance
            result[(int)Slot.Unequipped]      = _loadout[Slot.Unequipped];
            result[(int)Slot.PrimaryTool]     = _loadout[Slot.PrimaryTool];
            result[(int)Slot.SecondaryTool]   = _loadout[Slot.SecondaryTool];
            result[(int)Slot.PrimaryGadget]   = _loadout[Slot.PrimaryGadget];
            result[(int)Slot.SecondaryGadget] = _loadout[Slot.SecondaryGadget];
            result[(int)Slot.TertiaryGadget]  = _loadout[Slot.TertiaryGadget]; 
            return result;
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

        public static void SwapEquipment(this IEquipmentSwappable swapper, Transform lockerT, Slot slot = Slot.PrimaryTool)
        {
            Equipment fromLocker = swapper.EquipmentLockers[lockerT],
                fromPlayer = PlayerInput.Instance.Loadout[slot];

            int lockerIndex = Array.IndexOf(swapper.Lockers, lockerT);

            PlayerInput.Instance.Loadout.PutEquipmentInSlot(slot, fromLocker);
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

        public static string GetLockerSwapDescription(Equipment fromLocker, Slot slot)
        {
            Equipment current = PlayerInput.Instance.Loadout[slot];
            
            if (fromLocker == current)
                return null;
            if (current == Equipment.EmptyHand)
                return "Pick up " + fromLocker.ToString();
            else if (fromLocker == Equipment.EmptyHand)
                return "Put away " + current.ToString();
            else
                return String.Format("Swap {0} for {1}", current.ToString(), fromLocker.ToString());
        }

        public static PromptInfo GetLockerPrompt(this IEquipmentSwappable swapper, Transform transform)
        {
            Equipment fromLocker = swapper.EquipmentLockers[transform];

            Prompts.SwapEquipmentHint.Description = GetLockerSwapDescription(fromLocker, Slot.PrimaryTool);

            if (Game.Current.Player.PackData.HasUpgrade(EVA.EVAUpgrade.Toolbelt))
            {
                Prompts.SwapEquipmentHint.SecondaryKey = "Q";
                Prompts.SwapEquipmentHint.SecondaryDescription = GetLockerSwapDescription(fromLocker, Slot.SecondaryTool);
            }

            if (Prompts.SwapEquipmentHint.Description != null || Prompts.SwapEquipmentHint.SecondaryDescription != null)
            {
                return Prompts.SwapEquipmentHint;
            }
            else
            {
                return null;
            }
        }
    }
}