using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using RedHomestead.EVA;
using System.Linq;
using System.Collections.Generic;
using RedHomestead.Electricity;
using RedHomestead.Economy;
using RedHomestead.GameplayOptions;
using RedHomestead.Scoring;
using RedHomestead.Simulation;
using RedHomestead.Geography;

namespace RedHomestead.Persistence
{
    public interface IDataContainer<D> where D : RedHomesteadData
    {
        D Data { get; set; }
    }

    public interface IFlexData
    {
        string Flex { get; set; }
    }
    
    public interface IFlexDataContainer<D, F> : IDataContainer<D> where D : RedHomesteadData, IFlexData where F : class, new()
    {
        F FlexData { get; set; }
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
        public PackData PackData;
        public int BankAccount;
        public int GremlinMissStreak;
        public bool GremlinChastised;
        public List<Order> EnRouteOrders;
        public float[] PerkProgress;
        public Equipment.Equipment[] Loadout;
        public BackerFinancing Financing;
        internal int WeeklyIncomeSeed;

        public PlayerData() { }
        public PlayerData(bool generateSeeds)
        {
            if (generateSeeds)
            {
                WeeklyIncomeSeed = new System.Random().Next(int.MinValue, int.MaxValue);
            }
        }

        protected override void BeforeMarshal(Transform t = null)
        {
            base.BeforeMarshal(t);
            this.PackData = SurvivalTimer.Instance.Data;
            this.Loadout = PlayerInput.Instance.Loadout.MarshalLoadout();
        }
    }

    [Serializable]
    public class RoverData : FacingData
    {
        public bool HatchOpen;
        public EnergyContainer EnergyContainer;
        public string PowerableInstanceID;
        public float FaultedPercentage;
        public ResourceContainer Oxygen, Water;
    }

    public abstract class ConnectionData : FacingData
    {
        [HideInInspector]
        public Vector3 LocalScale;
        public Vector3 fromPos, toPos;
    }

    [Serializable]
    public class PipelineData : ConnectionData
    {
        [HideInInspector]
        public Simulation.Matter MatterType;
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

    public enum PowerlineType { Powerline = 0, Corridor, Umbilical }

    [Serializable]
    public class PowerlineData: ConnectionData, IFlexData
    {
        [NonSerialized]
        public IPowerable From, To;
        public Quaternion fromRot, toRot;
        public Vector3 fromScale, toScale;
        public string FromPowerableInstanceID, ToPowerableInstanceID;
        public PowerlineType Type;
        public string F;

        public string Flex { get { return F; } set { F = value; } }

        /// <summary>
        /// cached powerline for use when serializing
        /// </summary>
        private Powerline owner;

        protected override void BeforeMarshal(Transform t = null)
        {
            base.BeforeMarshal(t);

            if (From != null)
                FromPowerableInstanceID = From.PowerableInstanceID;
            if (To != null)
                ToPowerableInstanceID = To.PowerableInstanceID;

            LocalScale = t.localScale;

            if (owner == null && t != null)
                owner = t.GetComponent<Powerline>();

            if (owner is Corridor)
                Base.SerializeFlexData(this, owner as Corridor);
        }
    }

    [Serializable]
    public class EnvironmentData
    {
        public float CurrentHour = 9;
        public float CurrentMinute = 0;
        public int CurrentSol = 1;
        /// <summary>
        /// Normalized: 0 is no dust, 1 is full dust storm
        /// </summary>
        public float DustIntensity = 0f;

        public float HoursSinceSol0
        {
            get
            {
                return CurrentSol * SunOrbit.MartianHoursPerDay + CurrentHour;
            }
        }

        /// <summary>
        /// The normalized solar output at the current hour, based on a sine wave
        /// </summary>
        /// <returns>From 0 to 1f</returns>
        public float SolarIntensity()
        {
            //hand tuned function that sums to .211 kilo-watt-hours over x = 0 to 11
            //return Meter2PerModule * (((1 / 120)*x*(x*(x*(x*(11 * x - 130) + 525) - 950) + 1384)) + 24);
            //instead, we'll just use sine
            //this sin() is way off from the U shaped graph in reality
            //that means we're shortchanging players kWh
#warning inaccuracy: solar panel power generation over the course of a day
            float x;
            if (CurrentHour < 6)
            {
                return 0;
            }
            else if (CurrentHour < 12)
            {
                x = CurrentHour - 6;
            }
            else if (CurrentHour < 18)
            {
                x = CurrentHour - 6;
            }
            else
            {
                return 0;
            }

            return Mathf.Sin(Mathf.Lerp(0, Mathf.PI, x / 12));
        }
    }

    [Serializable]
    public class Base: ISerializationCallbackReceiver {
        public static Base Current;

        public string Name;
        public int WeatherSeed;
        public MarsRegion Region;
        public LatLong LatLong;
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
        public IceDrillData[] IceDrillData;
        public PowerCubeData[] PowerCubeData;
        public FacingData[] PumpData;
        public MobileSolarPanelData[] MobileSolarPanelData;
        public ToolboxData[] ToolboxData;
        public DepositData[] Deposits;
        public CargoLanderData[] CargoLanders;
        public LandingZoneData[] LandingZones;
        internal Dictionary<Simulation.Matter, int> InitialMatterPurchase;
        internal Dictionary<Crafting.Craftable, int> InitialCraftablePurchase;

        public Base() { }
        public Base(bool generateSeeds)
        {
            if (generateSeeds)
            {
                WeatherSeed = new System.Random().Next(int.MinValue, int.MaxValue);
            }
        }

        public void OnAfterDeserialize()
        {
            DeserializeDeposits();
            DeserializeLandersAndLZs();
            Dictionary<string, IPowerable> powerableMap = new Dictionary<string, IPowerable>();
            UnityEngine.Debug.Log("deserializing crates");
            DeserializeCrates();
            DeserializeCratelikes(powerableMap);
            UnityEngine.Debug.Log("deserializing con zones");
            _InstantiateMany<ConstructionZone, ConstructionData>(ConstructionZones, ModuleBridge.Instance.ConstructionZonePrefab);
            UnityEngine.Debug.Log("deserializing modules");
            DeserializeRover(powerableMap);
            DeserializeModules(powerableMap);
        }

        private void DeserializeLandersAndLZs()
        {
            _DestroyCurrent<LandingZone>();
            _InstantiateMany<LandingZone, LandingZoneData>(LandingZones, ModuleBridge.Instance.LandingZonePrefab);
            
            _DestroyCurrent<CargoLander>();
            _InstantiateMany<CargoLander, CargoLanderData>(CargoLanders, LandingZone.Instance.landerPrefab);
        }

        private void DeserializeDeposits()
        {
            _DestroyCurrent<Deposit>();
            _InstantiateMany<Deposit, DepositData>(Deposits, ModuleBridge.Instance.WaterDepositPrefab);
        }

        private void DeserializeRover(Dictionary<string, IPowerable> powerableMap)
        {
            Rovers.RoverInput rovIn = GameObject.FindObjectOfType<Rovers.RoverInput>();
            rovIn.Data = this.RoverData;
            rovIn.transform.position = this.RoverData.Position;
            rovIn.transform.rotation = this.RoverData.Rotation;
            powerableMap.Add(rovIn.PowerableInstanceID, rovIn);
        }

        private void DeserializeModules(Dictionary<string, IPowerable> powerableMap)
        {
            _DestroyCurrent<ResourcelessGameplay>();
            _DestroyCurrent<SingleResourceModuleGameplay>();
            _DestroyCurrent<MultipleResourceModuleGameplay>(typeof(Habitat));
            Habitat[] allHabs = Transform.FindObjectsOfType<Habitat>().Where(x => x.isActiveAndEnabled).ToArray();

            Dictionary<string, ModuleGameplay> moduleMap = new Dictionary<string, ModuleGameplay>();

#if UNITY_EDITOR
            reflectionRuns = 0;
            reflectionCheckTime.Reset();
            reflectionPropertyTime.Reset();
#endif

            //we have to look at each data to figure out which prefab to use
            foreach (ResourcelessModuleData data in ResourcelessData)
            {
                Transform t = GameObject.Instantiate(ModuleBridge.Instance.Modules[(int)data.ModuleType], data.Position, data.Rotation) as Transform;

                ResourcelessGameplay r = t.GetComponent<ResourcelessGameplay>();

                r.Data = data;

                moduleMap.Add(data.ModuleInstanceID, r);

                if (!String.IsNullOrEmpty(data.PowerableInstanceID))
                    powerableMap.Add(data.PowerableInstanceID, r);

                DeserializeFlexData(data, r);
            }
            foreach (SingleResourceModuleData data in SingleResourceContainerData)
            {
                Transform t = GameObject.Instantiate(ModuleBridge.Instance.Modules[(int)data.ModuleType], data.Position, data.Rotation) as Transform;

                SingleResourceModuleGameplay r = t.GetComponent<SingleResourceModuleGameplay>();

                r.Data = data;

                moduleMap.Add(data.ModuleInstanceID, r);

                if (!String.IsNullOrEmpty(data.PowerableInstanceID))
                    powerableMap.Add(data.PowerableInstanceID, r);

                DeserializeFlexData(data, r);
            }
            foreach (MultipleResourceModuleData data in MultiResourceContainerData)
            {
                if (data.ModuleType == Buildings.Module.Habitat)
                {
                    SyncToHabitat(allHabs, data, moduleMap, powerableMap);
                }
                else
                {
                    Transform t = GameObject.Instantiate(ModuleBridge.Instance.Modules[(int)data.ModuleType], data.Position, data.Rotation) as Transform;

                    MultipleResourceModuleGameplay r = t.GetComponent<MultipleResourceModuleGameplay>();

                    r.Data = data;

                    moduleMap.Add(data.ModuleInstanceID, r);

                    if (!String.IsNullOrEmpty(data.PowerableInstanceID))
                        powerableMap.Add(data.PowerableInstanceID, r);

                    DeserializeFlexData(data, r);
                }
            }
            foreach (PipelineData data in PipeData)
            {
                Transform t = GameObject.Instantiate(PlayerInput.Instance.gasPipePrefab, data.Position, data.Rotation) as Transform;
                t.localScale = data.LocalScale;

                Pipe r = t.GetComponent<Pipe>();
                r.Data = data;
                r.AssignConnections(data.MatterType, moduleMap[data.FromModuleInstanceID], moduleMap[data.ToModuleInstanceID], null, null);
            }
            foreach (PowerlineData data in PowerData)
            {
                Transform prefab = null;
                switch (data.Type)
                {
                    default:
                    case PowerlineType.Powerline:
                        prefab = PlayerInput.Instance.powerlinePrefab;
                        break;
                    case PowerlineType.Umbilical:
                        prefab = PlayerInput.Instance.umbilicalPrefab;
                        break;
                    case PowerlineType.Corridor:
                        prefab = PlayerInput.Instance.tubePrefab;
                        break;
                }

                Transform t = GameObject.Instantiate(prefab, data.Position, data.Rotation) as Transform;
                t.localScale = data.LocalScale;

                Powerline r = t.GetComponent<Powerline>();
                r.Data = data;
                DeserializeFlexData(data, r);
                r.AssignConnections(powerableMap[data.FromPowerableInstanceID], powerableMap[data.ToPowerableInstanceID], null, null);
            }
#if UNITY_EDITOR
            UnityEngine.Debug.Log(String.Format("deserialize reflection: {0} run, type checks: {1}ms, property reflection: {2}ms", reflectionRuns, reflectionCheckTime.ElapsedMilliseconds, reflectionPropertyTime.ElapsedMilliseconds));
#endif
        }

#if UNITY_EDITOR
        private static System.Diagnostics.Stopwatch reflectionCheckTime = new System.Diagnostics.Stopwatch();
        private static System.Diagnostics.Stopwatch reflectionPropertyTime = new System.Diagnostics.Stopwatch();
        private static int reflectionRuns = 0;
#endif

        public static void SerializeFlexData(IFlexData data, object containerScript)
        {
#if UNITY_EDITOR
            reflectionRuns++;
            reflectionCheckTime.Start();
#endif
            Type gameplayType = containerScript.GetType();
            if (IsSubclassOfRawGeneric(typeof(IFlexDataContainer<,>), gameplayType))
            {
#if UNITY_EDITOR
                reflectionCheckTime.Stop();
                reflectionPropertyTime.Start();
#endif
                System.Reflection.PropertyInfo pi = gameplayType.GetProperty("FlexData");
                data.Flex = JsonUtility.ToJson(pi.GetValue(containerScript, null));
#if UNITY_EDITOR
                reflectionPropertyTime.Stop();
#endif
            }
            else
            {
#if UNITY_EDITOR
                reflectionCheckTime.Stop();
#endif
            }
        }

        public static void DeserializeFlexData(IFlexData data, object containerScript)
        {
#if UNITY_EDITOR
            reflectionRuns++;
            reflectionCheckTime.Start();
#endif
            Type gameplayType = containerScript.GetType();
            if (IsSubclassOfRawGeneric(typeof(IFlexDataContainer<,>), gameplayType))
            {
#if UNITY_EDITOR
                reflectionCheckTime.Stop();
                reflectionPropertyTime.Start();
#endif
                System.Reflection.PropertyInfo pi = gameplayType.GetProperty("FlexData");
                object deserialized = JsonUtility.FromJson(data.Flex, pi.PropertyType);
                pi.SetValue(containerScript, deserialized, null);
#if UNITY_EDITOR
                reflectionPropertyTime.Stop();
#endif
            }
            else
            {
#if UNITY_EDITOR
                reflectionCheckTime.Stop();
#endif
            }
        }

        /// <summary>
        /// Given a habitat module data and a list of all inscene habs
        /// Finds the matching habitat and the matching hab extra data
        /// And sets both data objects to the habitat script
        /// </summary>
        /// <param name="allHabs"></param>
        /// <param name="data"></param>
        private void SyncToHabitat(Habitat[] allHabs, MultipleResourceModuleData data, Dictionary<string, ModuleGameplay> moduleMap, Dictionary<string, IPowerable> powerableMap)
        {
            HabitatExtraData matchingHabData = Habitats.FirstOrDefault(x => x.ModuleInstanceID == data.ModuleInstanceID);
            Habitat matchingHab = allHabs.FirstOrDefault(x => x.name == data.ModuleInstanceID);
            matchingHab.Data = data;
            matchingHab.HabitatData = matchingHabData;

            if (matchingHab.OnResourceChange != null)
                matchingHab.OnResourceChange(Simulation.Matter.Biomass, Simulation.Matter.OrganicMeals, Simulation.Matter.MealShakes, Simulation.Matter.RationMeals, Simulation.Matter.MealPowders);

            moduleMap.Add(data.ModuleInstanceID, matchingHab);
            powerableMap.Add(data.PowerableInstanceID, matchingHab);
        }

        private void DeserializeCratelikes(Dictionary<string, IPowerable> powerableMap)
        {
            _DestroyCurrent<IceDrill>();
            _InstantiateMany<IceDrill, IceDrillData>(IceDrillData, ModuleBridge.Instance.IceDrillPrefab, (IceDrill drill, IceDrillData data) => {
                powerableMap.Add(data.PowerableInstanceID, drill);
            });

            _DestroyCurrent<PowerCube>();
            _InstantiateMany<PowerCube, PowerCubeData>(PowerCubeData, ModuleBridge.Instance.PowerCubePrefab, (PowerCube cube, PowerCubeData data) => {
                powerableMap.Add(data.PowerableInstanceID, cube);
            });

            _DestroyCurrent<MobileSolarPanel>();
            _InstantiateMany<MobileSolarPanel, MobileSolarPanelData>(MobileSolarPanelData, ModuleBridge.Instance.MobileSolarPanelPrefab, (MobileSolarPanel panel, MobileSolarPanelData data) => {
                powerableMap.Add(data.PowerableInstanceID, panel);
            });

            _DestroyCurrent<Pump>();
            _InstantiateMany<Pump, FacingData>(PumpData, ModuleBridge.Instance.PumpPrefab);

            _DestroyCurrent<Toolbox>();
            _InstantiateMany<Toolbox, ToolboxData>(ToolboxData, ModuleBridge.Instance.ToolboxPrefab);
        }

        private void DeserializeCrates()
        {
            _DestroyCurrent<ResourceComponent>();

            //this would be
            //_InstantiateMany<ResourceComponent, CrateData>(Crates, EconomyManager.Instance.GetResourceCratePrefab(data.ResourceType));
            //but we have to look at each data to figure out which prefab to use
            foreach (CrateData data in Crates)
            {
                Transform t = GameObject.Instantiate(EconomyManager.Instance.GetResourceCratePrefab(data.Container.MatterType), data.Position, data.Rotation) as Transform;
                ResourceComponent r = t.GetComponent<ResourceComponent>();
                r.Data = data;
                if (r.Data.Container.Size == ContainerSize.Quarter)
                    t.localScale = BounceLander.HalfSizeQuarterVolume;
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

        private void _InstantiateMany<T, D>(D[] list, Transform prefab, Action<T, D> rider = null) where T : MonoBehaviour, IDataContainer<D> where D : FacingData
        {
            foreach (D data in list)
            {
                Transform t = GameObject.Instantiate(prefab, data.Position, data.Rotation) as Transform;
                T c = t.GetComponent<T>();
                c.Data = data;
                if (rider != null)
                    rider(c, data);
            }
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            reflectionRuns = 0;
            reflectionCheckTime.Reset();
            reflectionPropertyTime.Reset();
#endif
            this._MarshalManyFromScene<ResourceComponent, CrateData>((crates) => this.Crates = crates);
            this._MarshalManyFromScene<ConstructionZone, ConstructionData>((zones) => this.ConstructionZones = zones);
            this._MarshalManyFromScene<ResourcelessGameplay, ResourcelessModuleData>((modules) => this.ResourcelessData = modules);
            this._MarshalManyFromScene<MultipleResourceModuleGameplay, MultipleResourceModuleData>((modules) => this.MultiResourceContainerData = modules);
            this._MarshalManyFromScene<SingleResourceModuleGameplay, SingleResourceModuleData>((modules) => this.SingleResourceContainerData = modules);
            this._MarshalManyFromScene<Pipe, PipelineData>((pipes) => this.PipeData = pipes);
            this._MarshalManyFromScene<Powerline, PowerlineData>((powerline) => this.PowerData = powerline);
            this._MarshalManyFromScene<IceDrill, IceDrillData>((drill) => this.IceDrillData = drill);
            this._MarshalManyFromScene<PowerCube, PowerCubeData>((power) => this.PowerCubeData = power);
            this._MarshalManyFromScene<MobileSolarPanel, MobileSolarPanelData>((panel) => this.MobileSolarPanelData = panel);
            this._MarshalManyFromScene<Pump, FacingData>((pump) => this.PumpData = pump);
            this._MarshalManyFromScene<Toolbox, ToolboxData>((box) => this.ToolboxData = box);
            this._MarshalManyFromScene<Deposit, DepositData>((dep) => this.Deposits = dep);
            this._MarshalManyFromScene<CargoLander, CargoLanderData>((lander) => this.CargoLanders = lander);
            this._MarshalManyFromScene<LandingZone, LandingZoneData>((lz) => this.LandingZones = lz);

            this._MarshalHabitats();

            Rovers.RoverInput rovIn = GameObject.FindObjectOfType<Rovers.RoverInput>();
            if (rovIn != null)
                this.RoverData = (RoverData)rovIn.Data.Marshal(rovIn.transform);

#if UNITY_EDITOR
            UnityEngine.Debug.Log(String.Format("serialize reflection: {0} run, type checks: {1}ms, property reflection: {2}ms", reflectionRuns, reflectionCheckTime.ElapsedMilliseconds, reflectionPropertyTime.ElapsedMilliseconds));
#endif
        }

        private void _MarshalHabitats()
        {
            this.Habitats = Array.ConvertAll(Transform.FindObjectsOfType<Habitat>().Where(x => x.isActiveAndEnabled).ToArray(), hab => (HabitatExtraData)hab.HabitatData.Marshal(hab.transform));
        }

        private void _MarshalManyFromScene<C, D>(Action<D[]> setter) where C : MonoBehaviour, IDataContainer<D> where D : RedHomesteadData
        {
            setter(Array.ConvertAll(Transform.FindObjectsOfType<C>().Where(x => x.isActiveAndEnabled).ToArray(),
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

        private static Queue<Type> typeQueue = new Queue<Type>();
        //https://stackoverflow.com/questions/457676/check-if-a-class-is-derived-from-a-generic-class
        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            typeQueue.Clear();
            while (toCheck != null && toCheck != typeof(object))
            {
                var concreteOrGenericType = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == concreteOrGenericType)
                {
                    return true;
                }
                foreach(Type @interface in concreteOrGenericType.GetInterfaces())
                {
                    typeQueue.Enqueue(@interface);
                }
                typeQueue.Enqueue(toCheck.BaseType);
                toCheck = typeQueue.Dequeue();
            }
            return false;
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
        public GameScore Score;
        public int GetScore()
        {
            return this.Score.GetScore(Environment.CurrentSol, Environment.CurrentHour);
        }

        /// <summary>
        /// Marked internal+property to prevent loading from Serialization
        /// </summary>
        internal bool IsNewGame { get; set; }
        /// <summary>
        /// Marked internal+property to prevent loading from Serialization
        /// </summary>
        internal bool IsTutorial { get; set; }

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
            Perks.PerkMultipliers.LoadFromPlayerPerkProgress();
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
        
        public static void StartNewGame(NewGameChoices choices)
        {
            if (!choices.IsTutorial)
            {
                choices.AddMinimumSupplies();
                choices.AddBackerSupplies();
            }

            UnityEngine.Debug.Log("Starting new game");
            Game.Current = new Game()
            {
                IsNewGame = true,
                IsTutorial = choices.IsTutorial,
                Bases = new Base[]
                {
                    new Base(true)
                    {
                        Name = choices.HomesteadName,
                        Region = choices.ChosenLocation.Region,
                        LatLong = choices.ChosenLocation.LatLong,
                        InitialMatterPurchase = choices.BoughtMatter,
                        InitialCraftablePurchase = choices.BoughtCraftables,
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
                Player = new PlayerData(true)
                {
                    Name = choices.PlayerName,
                    BankAccount = choices.RemainingFunds,
                    Financing = choices.ChosenFinancing,
                    PackData = EVA.EVA.GetDefaultPackData(),
                    EnRouteOrders = new List<Order>(),
                    PerkProgress = choices.GetPerkProgress(),
                    Loadout = new Equipment.Equipment[]
                    {
                        Equipment.Equipment.ChemicalSniffer, //secondary gadget
                        Equipment.Equipment.Blueprints, //primary gadget
                        Equipment.Equipment.Locked, //tertiary gadget
                        Equipment.Equipment.Locked, //secondary tool
                        Equipment.Equipment.EmptyHand, //unequipped
                        Equipment.Equipment.PowerDrill, //primary tool
                    }
                },
                Score = new GameScore(),
                History = new Simulation.GlobalHistory()
            };
            Base.Current = Game.Current.Bases[0];
            Perks.PerkMultipliers.LoadFromPlayerPerkProgress();
        }
    }
}
