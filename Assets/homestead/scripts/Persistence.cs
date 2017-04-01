using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RedHomestead.EVA;
using System.Linq;
using System.Collections.Generic;

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
    public class RoverData : FacingData
    {
        public bool HatchOpen;
    }

    public abstract class ConnectionData : FacingData
    {
        [HideInInspector]
        public Vector3 LocalScale;
        [NonSerialized]
        public ModuleGameplay From, To;
        public string FromModuleInstanceID, ToModuleInstanceID;

        protected override void BeforeMarshal(Transform t = null)
        {
            base.BeforeMarshal(t);

            if (From != null)
                FromModuleInstanceID = From.ModuleInstanceID;
            if (To != null)
                ToModuleInstanceID = To.ModuleInstanceID;

            LocalScale = t.localScale;
        }
    }

    [Serializable]
    public class PipelineData : ConnectionData
    {
        [HideInInspector]
        public Simulation.Matter MatterType;
    }

    [Serializable]
    public class PowerlineData: ConnectionData { }

    [Serializable]
    public class EnvironmentData
    {
        public float CurrentHour = 9;
        public float CurrentMinute = 0;
        public int CurrentSol = 1;

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
        public HabitatExtraData[] Habitats;
        public ConstructionData[] ConstructionZones;
        //hobbit hole data
        //floorplan data
        //stuff data
        public ResourcelessModuleData[] ResourcelessData;
        public MultipleResourceModuleData[] MultiResourceContainerData;
        public SingleResourceModuleData[] SingleResourceContainerData;
        public RoverData RoverData;
        public PipelineData[] PipeData;
        public PowerlineData[] PowerData;
        ////pipe data

        public void OnAfterDeserialize()
        {
            UnityEngine.Debug.Log("creating crates");
            DeserializeCrates();
            UnityEngine.Debug.Log("creating con zones");
            _InstantiateMany<ConstructionZone, ConstructionData>(ConstructionZones, ModuleBridge.Instance.ConstructionZonePrefab);
            UnityEngine.Debug.Log("creating modules");
            DeserializeModules();
            DeserializeRover();
        }

        private void DeserializeRover()
        {
            Rovers.RoverInput rovIn = GameObject.FindObjectOfType<Rovers.RoverInput>();
            rovIn.Data = this.RoverData;
            rovIn.transform.position = this.RoverData.Position;
            rovIn.transform.rotation = this.RoverData.Rotation;
        }

        private void DeserializeModules()
        {
            _DestroyCurrent<ResourcelessGameplay>();
            _DestroyCurrent<SingleResourceModuleGameplay>();
            _DestroyCurrent<MultipleResourceModuleGameplay>(typeof(Habitat));
            Habitat[] allHabs = Transform.FindObjectsOfType<Habitat>();

            Dictionary<string, ModuleGameplay> moduleMap = new Dictionary<string, ModuleGameplay>();

            //we have to look at each data to figure out which prefab to use
            foreach (ResourcelessModuleData data in ResourcelessData)
            {
                Transform t = GameObject.Instantiate(ModuleBridge.Instance.Modules[(int)data.ModuleType], data.Position, data.Rotation) as Transform;
                ResourcelessGameplay r = t.GetComponent<ResourcelessGameplay>();
                r.Data = data;
                moduleMap.Add(data.ModuleInstanceID, r);
            }
            foreach (SingleResourceModuleData data in SingleResourceContainerData)
            {
                Transform t = GameObject.Instantiate(ModuleBridge.Instance.Modules[(int)data.ModuleType], data.Position, data.Rotation) as Transform;
                SingleResourceModuleGameplay r = t.GetComponent<SingleResourceModuleGameplay>();
                r.Data = data;
                moduleMap.Add(data.ModuleInstanceID, r);
            }
            foreach (MultipleResourceModuleData data in MultiResourceContainerData)
            {
                if (data.ModuleType == Buildings.Module.Habitat)
                {
                    SyncToHabitat(allHabs, data, moduleMap);
                }
                else
                {
                    Transform t = GameObject.Instantiate(ModuleBridge.Instance.Modules[(int)data.ModuleType], data.Position, data.Rotation) as Transform;
                    MultipleResourceModuleGameplay r = t.GetComponent<MultipleResourceModuleGameplay>();
                    r.Data = data;
                    moduleMap.Add(data.ModuleInstanceID, r);
                }
            }
            foreach (PipelineData data in PipeData)
            {
                Transform t = GameObject.Instantiate(PlayerInput.Instance.gasPipePrefab, data.Position, data.Rotation) as Transform;
                t.localScale = data.LocalScale;

                Pipe r = t.GetComponent<Pipe>();
                r.Data = data;
                r.AssignConnections(data.MatterType, moduleMap[data.FromModuleInstanceID], moduleMap[data.ToModuleInstanceID]);
            }
            foreach (PowerlineData data in PowerData)
            {
                Transform t = GameObject.Instantiate(PlayerInput.Instance.powerlinePrefab, data.Position, data.Rotation) as Transform;
                t.localScale = data.LocalScale;

                Powerline r = t.GetComponent<Powerline>();
                r.Data = data;
                r.AssignConnections(moduleMap[data.FromModuleInstanceID], moduleMap[data.ToModuleInstanceID]);
            }
        }

        /// <summary>
        /// Given a habitat module data and a list of all inscene habs
        /// Finds the matching habitat and the matching hab extra data
        /// And sets both data objects to the habitat script
        /// </summary>
        /// <param name="allHabs"></param>
        /// <param name="data"></param>
        private void SyncToHabitat(Habitat[] allHabs, MultipleResourceModuleData data, Dictionary<string, ModuleGameplay> moduleMap)
        {
            HabitatExtraData matchingHabData = Habitats.FirstOrDefault(x => x.ModuleInstanceID == data.ModuleInstanceID);
            Habitat matchingHab = allHabs.FirstOrDefault(x => x.Data.ModuleInstanceID == data.ModuleInstanceID);
            matchingHab.Data = data;
            matchingHab.HabitatData = matchingHabData;

            if (matchingHab.OnResourceChange != null)
                matchingHab.OnResourceChange(Simulation.Matter.Biomass, Simulation.Matter.OrganicMeal, Simulation.Matter.MealShake, Simulation.Matter.RationMeal, Simulation.Matter.MealPowder);

            moduleMap.Add(data.ModuleInstanceID, matchingHab);
        }

        private void DeserializeCrates()
        {
            _DestroyCurrent<ResourceComponent>();

            //this would be
            //_InstantiateMany<ResourceComponent, CrateData>(Crates, EconomyManager.Instance.GetResourceCratePrefab(data.ResourceType));
            //but we have to look at each data to figure out which prefab to use
            foreach (CrateData data in Crates)
            {
                Transform t = GameObject.Instantiate(EconomyManager.Instance.GetResourceCratePrefab(data.ResourceType), data.Position, data.Rotation) as Transform;
                ResourceComponent r = t.GetComponent<ResourceComponent>();
                r.Data = data;
            }
        }

        private void _DestroyCurrent<T>(Type exceptThisType = null) where T : MonoBehaviour
        {
            T[] things = UnityEngine.Transform.FindObjectsOfType<T>();
            for (int i = things.Length - 1; i > -1; i--)
            {
                if (exceptThisType != null && things[i].GetType() == exceptThisType)
                {
                    //noop
                }
                else
                {
                    GameObject.Destroy(things[i].gameObject);
                }
            }
        }

        private void _InstantiateMany<T, D>(D[] list, Transform prefab) where T : MonoBehaviour, IDataContainer<D> where D : FacingData
        {
            foreach (D data in list)
            {
                Transform t = GameObject.Instantiate(prefab, data.Position, data.Rotation) as Transform;
                T c = t.GetComponent<T>();
                c.Data = data;
            }
        }

        public void OnBeforeSerialize()
        {
            this._MarshalManyFromScene<ResourceComponent, CrateData>((crates) => this.Crates = crates);
            this._MarshalManyFromScene<ConstructionZone, ConstructionData>((zones) => this.ConstructionZones = zones);
            this._MarshalManyFromScene<ResourcelessGameplay, ResourcelessModuleData>((modules) => this.ResourcelessData = modules);
            this._MarshalManyFromScene<MultipleResourceModuleGameplay, MultipleResourceModuleData>((modules) => this.MultiResourceContainerData = modules);
            this._MarshalManyFromScene<SingleResourceModuleGameplay, SingleResourceModuleData>((modules) => this.SingleResourceContainerData = modules);
            this._MarshalManyFromScene<Pipe, PipelineData>((pipes) => this.PipeData = pipes);
            this._MarshalManyFromScene<Powerline, PowerlineData>((powerline) => this.PowerData = powerline);

            this._MarshalHabitats();

            Rovers.RoverInput rovIn = GameObject.FindObjectOfType<Rovers.RoverInput>();
            if (rovIn != null)
                this.RoverData = (RoverData)rovIn.Data.Marshal(rovIn.transform);
        }

        private void _MarshalHabitats()
        {
            this.Habitats = Array.ConvertAll(Transform.FindObjectsOfType<Habitat>(), hab => (HabitatExtraData)hab.HabitatData.Marshal(hab.transform));
        }

        private void _MarshalManyFromScene<C, D>(Action<D[]> setter) where C : MonoBehaviour, IDataContainer<D> where D : RedHomesteadData
        {
            setter(Array.ConvertAll(Transform.FindObjectsOfType<C>(),
                container =>
                {
                    if (container == null)
                        UnityEngine.Debug.LogWarning("Null container for data type: " + typeof(D).ToString());
                    else if (container.transform == null)
                        UnityEngine.Debug.LogWarning("Null transform for data type: " + typeof(D).ToString());
                    else if (container.Data == null)
                        UnityEngine.Debug.LogWarning(container.transform.name + " gameobject has no Data of type: " + typeof(D).ToString());

                    return (D)container.Data.Marshal(container.transform);
                }
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

        internal bool IsNewGame { get; set; }

        public void OnBeforeSerialize()
        {
            this.Player = Player.Marshal(PlayerInput.Instance.transform.root) as PlayerData;
        }

        public void OnAfterDeserialize()
        {
            this.Player.AfterDeserialize(PlayerInput.Instance.transform.root);
            UnityEngine.Debug.Log("Finished deserializing game");
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

        private static Game _LoadGame(string playerName)
        {
            return JsonUtility.FromJson<Game>(File.ReadAllText(Path.Combine(GetSavesFolderPath(), GetGameFileName(playerName))));
        }

        public static void LoadGame(string playerName)
        {
            Game.Current = _LoadGame(playerName);
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
            UnityEngine.Debug.Log("Starting new game");
            Game.Current = new Game()
            {
                IsNewGame = true,
                Bases = new Base[]
                {
                    new Base()
                    {
                        Crates = new CrateData[] { },
                        Habitats = new HabitatExtraData[] { },
                        ConstructionZones = new ConstructionData[] { },
                        ResourcelessData = new ResourcelessModuleData[] { },
                        SingleResourceContainerData = new SingleResourceModuleData[] { },
                        MultiResourceContainerData = new MultipleResourceModuleData[] { },
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
