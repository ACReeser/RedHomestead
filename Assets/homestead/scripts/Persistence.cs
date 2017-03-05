using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RedHomestead.EVA;

namespace RedHomestead.Persistence
{
    public interface IDataContainer<D> where D : RedHomesteadData
    {
        D Data { get; set; }
    }

    [Serializable]
    public abstract class RedHomesteadData
    {
        protected abstract void BeforeMarshal(UnityEngine.MonoBehaviour container);
        public RedHomesteadData Marshal(UnityEngine.MonoBehaviour container)
        {
            this.BeforeMarshal(container);
            return this;
        }
    }

    [Serializable]
    public class FacingData : RedHomesteadData
    {
        [HideInInspector]
        public Vector3 Position;
        [HideInInspector]
        public Quaternion Rotation;

        protected override void BeforeMarshal(MonoBehaviour container)
        {
            Position = container.transform.position;
            Rotation = container.transform.rotation;
        }
    }

    [Serializable]
    public class PlayerData: FacingData
    {
        public string Name;
        public float ExcavationPerSecond;
        public float ConstructionPerSecond = 1f;
        public PackData PackData;
        public int BankAccount;

        protected override void BeforeMarshal(MonoBehaviour container)
        {
            base.BeforeMarshal(container);
            this.PackData = SurvivalTimer.Instance.Data;
        }
    }

    [Serializable]
    public class EnvironmentData
    {
        internal float CurrentHour = 9;
        internal float CurrentMinute = 0;
        internal int CurrentSol = 1;

        public float HoursSinceSol0
        {
            get
            {
                return CurrentSol * SunOrbit.MartianHoursPerDay + CurrentHour;
            }
        }
    }

    [Serializable]
    public class Base: ISerializationCallbackReceiver {
        public static Base Current;

        public CrateData[] Crates;
        public HabitatData[] Habitats;
        //hobbit hole data
        //floorplan data
        //stuff data
        //module data
        ////container data
        ////pipe data

        public void Marshal()
        {
        }

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
            this._MarshalManyFromScene<ResourceComponent, CrateData>((crates) => this.Crates = crates);
            this._MarshalManyFromScene<Habitat, HabitatData>((habitats) => this.Habitats = habitats);
        }

        private void _MarshalManyFromScene<C, D>(Action<D[]> setter) where C : UnityEngine.MonoBehaviour, IDataContainer<D> where D : RedHomesteadData
        {
            setter(Array.ConvertAll(UnityEngine.Transform.FindObjectsOfType<C>(),
                container =>
                (D)container.Data.Marshal(container)
            ));
        }
    }

    [Serializable]
    public class Game : ISerializationCallbackReceiver
    {
        public static Game Current { get; set; }

        public EnvironmentData Environment;
        public PlayerData Player;
        public Base[] Bases;

        public void OnBeforeSerialize()
        {
            this.Player = Player.Marshal(PlayerInput.Instance) as PlayerData;
        }

        public void OnAfterDeserialize()
        {
            throw new NotImplementedException();
        }
    }

    public static class PersistentDataManager
    {
        public const string baseGameFileName = "game.json";
        public static BinaryFormatter _formatter = new BinaryFormatter();

        public static void SaveGame(Game gameToSave)
        {
            try
            {
                string json = JsonUtility.ToJson(gameToSave);
                File.WriteAllText(Path.Combine(UnityEngine.Application.persistentDataPath, GetGameFileName(gameToSave.Player.Name)), json);
            }
            catch(Exception e)
            {
                UnityEngine.Debug.LogError(e.ToString());
            }

            //todo: save slot file
        }

        private static string GetGameFileName(string name)
        {
            return String.Format("{0}.{1}", name, baseGameFileName);
        }

        public static Game LoadGame(string playerName)
        {
            return JsonUtility.FromJson<Game>(File.ReadAllText(Path.Combine(UnityEngine.Application.persistentDataPath, GetGameFileName(playerName))));
        }

        public static string[] GetPlayerNames()
        {
            throw new NotImplementedException();
        }

        public static string GetLastPlayedPlayerName()
        {
            throw new NotImplementedException();
        }

        //todo: pass in perk/equipment selections from screen
        public static void StartNewGame()
        {
            Game.Current = new Game()
            {
                Bases = new Base[]
                {
                    new Base()
                    {
                        Crates = new CrateData[] { },
                        Habitats = new HabitatData[] { }
                    }
                },
                Environment = new EnvironmentData()
                {
                    CurrentHour = 9,
                    CurrentMinute = 0,
                    CurrentSol = 0
                },
                Player = new PlayerData()
                {
                    Name = "Ares",
                    BankAccount = 350000,
                    ExcavationPerSecond = 1f,
                    ConstructionPerSecond = 1f,
                    PackData = EVA.EVA.GetDefaultPackData()
                }
            };
        }
    }

}
