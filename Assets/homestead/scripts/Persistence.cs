using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RedHomestead.EVA;
using System.Linq;

namespace RedHomestead.Persistence
{
    public interface IDataContainer<D> where D : RedHomesteadData
    {
        D Data { get; set; }
    }

    [Serializable]
    public abstract class RedHomesteadData
    {
        protected abstract void BeforeMarshal(Transform t);
        public abstract void AfterDeserialize(Transform t = null);

        public RedHomesteadData Marshal(Transform t)
        {
            this.BeforeMarshal(t);
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

        [NonSerialized]
        internal Transform Transform;

        public override void AfterDeserialize(Transform t)
        {
            Transform = t;
            Transform.position = this.Position;
            Transform.rotation = this.Rotation;
        }

        protected override void BeforeMarshal(Transform t = null)
        {
            Position = (t ?? this.Transform).position;
            Rotation = (t ?? this.Transform).rotation;
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

        protected override void BeforeMarshal(Transform t = null)
        {
            base.BeforeMarshal(t);
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
        
        public void OnAfterDeserialize()
        {
            UnityEngine.Debug.Log("creating crates");
            DeserializeCrates();
        }

        private void DeserializeCrates()
        {
            ResourceComponent[] starterCrates = UnityEngine.Transform.FindObjectsOfType<ResourceComponent>();
            for (int i = starterCrates.Length - 1; i > -1; i--)
            {
                GameObject.Destroy(starterCrates[i].gameObject);
            }

            foreach (CrateData data in Crates)
            {
                Transform t = GameObject.Instantiate(EconomyManager.Instance.GetResourceCratePrefab(data.ResourceType), data.Position, data.Rotation) as Transform;
                ResourceComponent r = t.GetComponent<ResourceComponent>();
                r.Data = data;
            }
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
                (D)container.Data.Marshal(container.transform)
            ));
        }
    }

    [Serializable]
    public class Game : ISerializationCallbackReceiver
    {
        public static Game Current { get; set; }
        
        public Simulation.GlobalHistory History;
        public EnvironmentData Environment;
        public PlayerData Player;
        public Base[] Bases;

        public void OnBeforeSerialize()
        {
            this.Player = Player.Marshal(PlayerInput.Instance.transform.root) as PlayerData;
        }

        public void OnAfterDeserialize()
        {
            this.Player.AfterDeserialize(PlayerInput.Instance.transform.root);
        }
    }

    public static class PersistentDataManager
    {
        public const string baseGameFileName = "game.json";
        public const string savesFolderName = "Saves";
        public static BinaryFormatter _formatter = new BinaryFormatter();

        public static void SaveGame(Game gameToSave)
        {
            try
            {
                string json = JsonUtility.ToJson(gameToSave);
                string savesFolderPath = GetSavesFolderPath();

                if (!Directory.Exists(savesFolderPath))
                {
                    Directory.CreateDirectory(savesFolderPath);
                }

                File.WriteAllText(Path.Combine(savesFolderPath, GetGameFileName(gameToSave.Player.Name)), json);
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

        private static string GetSavesFolderPath()
        {
            return Path.Combine(UnityEngine.Application.persistentDataPath, savesFolderName);
        }

        public static Game LoadGame(string playerName)
        {
            return JsonUtility.FromJson<Game>(File.ReadAllText(Path.Combine(GetSavesFolderPath(), GetGameFileName(playerName))));
        }

        public static string[] GetPlayerNames()
        {
            return new DirectoryInfo(GetSavesFolderPath()).GetFiles().OrderByDescending(x => x.LastWriteTimeUtc).Select(x => x.Name.Split('.')[0]).ToArray();
        }

        public static string GetLastPlayedPlayerName()
        {
            FileInfo lastFile = new DirectoryInfo(GetSavesFolderPath()).GetFiles().OrderByDescending(x => x.LastWriteTimeUtc).FirstOrDefault();

            if (lastFile != null)
            {
                return lastFile.Name.Split('.')[0];
            }
            else
            {
                return null;
            }
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
