using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;
using RedHomestead.Electricity;
using RedHomestead.Buildings;
using RedHomestead.Industry;

/// <summary>
/// Base abstract class for all modules
/// All modules are assumed to be powered, have a module type, and a ModuleInstanceID
/// </summary>
public abstract class ModuleData : FacingData
{ 
    [HideInInspector]
    public LocalEnergyHistory EnergyHistory = new LocalEnergyHistory();
    public RedHomestead.Buildings.Module ModuleType;
    public string ModuleInstanceID;
    public float FaultedPercentage;
    public string PowerableInstanceID { get { return ModuleInstanceID; } }

    /// <summary>
    /// Flex data
    /// </summary>
    public string Flex;
    /// <summary>
    /// cached MB for use when serializing
    /// </summary>
    private ModuleGameplay owner;

    protected override void BeforeMarshal(Transform t = null)
    {
        base.BeforeMarshal(t);

        if (owner == null)
            owner = t.GetComponent<ModuleGameplay>();

        Base.SerializeFlexData(this, owner);
    }
}

public abstract class ResourcefullModuleData: ModuleData
{
    [HideInInspector]
    public LocalMatterHistory MatterHistory = new LocalMatterHistory();
}

[Serializable]
public class ResourcelessModuleData : ModuleData { }

[Serializable]
public class ResourceContainerDictionary: SerializableDictionary<Matter, ResourceContainer> { }

[Serializable]
public class MultipleResourceModuleData: ResourcefullModuleData
{
    public ResourceContainerDictionary Containers;
}

[Serializable]
public class SingleResourceModuleData : ResourcefullModuleData
{
    public ResourceContainer Container;
}

public abstract class ModuleGameplay : MonoBehaviour, ISink, IPowerable, IBuildable, IPumpable
{
    protected AudioSource SoundSource;
    
    public bool HasPower { get; set; }
    
    public abstract string ModuleInstanceID { get; }

    #region power members
    public abstract float WattsConsumed { get; }
    public string PowerGridInstanceID { get; set; }
    public abstract string PowerableInstanceID { get; }
    public PowerVisualization powerViz;
    public PowerVisualization PowerViz { get { return powerViz; } }
    public virtual bool CanMalfunction { get { return true; } }
    #endregion

    #region repairable members
    public FailureAnchors failureEffectAnchors;
    public FailureAnchors FailureEffectAnchors { get { return failureEffectAnchors; } }
    public abstract float FaultedPercentage { get; set; }
    #endregion

    protected List<IPumpable> Adjacent = new List<IPumpable>();
    public List<IPumpable> AdjacentPumpables { get { return this.Adjacent; } }

    void Start()
    {
        SoundSource = this.GetComponent<AudioSource>();

        if (Game.Current.IsNewGame)
        {
#if UNITY_EDITOR
            print(this.GetType().ToString() + " loading up for new game");
#endif
            InitializeStartingData();
        }

        if (this.powerViz.IsAssigned)
            this.InitializePowerVisualization();

        Gremlin.Instance.Register(this as IRepairable);

        this.OnStart();
    }

    protected virtual string GetModuleInstanceID()
    {
        return Guid.NewGuid().ToString();
    }

    protected virtual void OnStart() { }
    public virtual void OnPowerChanged() { }
    public abstract void OnAdjacentChanged();
    public abstract void Tick();
    public abstract void Report();
    public abstract ResourceContainer Get(Matter c);
    public abstract bool HasContainerFor(Matter c);
    /// <summary>
    /// Initializes the Data object
    /// Called in exactly two places: on construction, and on new game start
    /// </summary>
    public abstract void InitializeStartingData();
    public abstract RedHomestead.Buildings.Module GetModuleType();
}

public abstract class ResourcelessGameplay : ModuleGameplay, IDataContainer<ResourcelessModuleData>
{
    [SerializeField]
    private ResourcelessModuleData data;
    public ResourcelessModuleData Data { get { return data; } set { data = value; } }
    public override string ModuleInstanceID { get { return data.ModuleInstanceID; } }
    public override string PowerableInstanceID { get { return data.PowerableInstanceID; }  }
    public override float FaultedPercentage { get { return data.FaultedPercentage; } set { data.FaultedPercentage = value; } }

    public override ResourceContainer Get(Matter c)
    {
        return null;
    }

    public override bool HasContainerFor(Matter c)
    {
        return false;
    }

    public override void InitializeStartingData()
    {
        this.Data = new ResourcelessModuleData()
        {
            ModuleInstanceID = GetModuleInstanceID(),
            EnergyHistory = new LocalEnergyHistory(),
            ModuleType = GetModuleType(),
        };
    }
}

public abstract class ResourcelessHabitatGameplay: ResourcelessGameplay, IHabitatModule
{
    [HideInInspector]
    public List<IHabitatModule> AdjacentModules { get; set; }
    [HideInInspector]
    public Habitat LinkedHabitat { get; set; }

    public Transform[] bulkheads;
    public Transform[] Bulkheads { get { return bulkheads; } }

    public override bool CanMalfunction
    {
        get
        {
            if (LinkedHabitat == null)
                return false;

            return base.CanMalfunction;
        }
    }
}

public abstract class MultipleResourceModuleGameplay: ModuleGameplay, IDataContainer<MultipleResourceModuleData>
{
    [SerializeField]
    private MultipleResourceModuleData data;
    public MultipleResourceModuleData Data { get { return data; } set { data = value; } }
    public override string ModuleInstanceID { get { return data.ModuleInstanceID; } }
    public override string PowerableInstanceID { get { return data.PowerableInstanceID; } }
    public override float FaultedPercentage { get { return data.FaultedPercentage; } set { data.FaultedPercentage = value; } }

    public override ResourceContainer Get(Matter c)
    {
        if (Data.Containers.ContainsKey(c))
            return data.Containers[c];
        else
            return null;
    }

    public override bool HasContainerFor(Matter c)
    {
        return Data.Containers.ContainsKey(c);
    }

    public override void InitializeStartingData()
    {
        this.Data = new MultipleResourceModuleData()
        {
            ModuleInstanceID = GetModuleInstanceID(),
            EnergyHistory = new LocalEnergyHistory(),
            MatterHistory = new LocalMatterHistory(),
            ModuleType = GetModuleType(),
            Containers = GetStartingDataContainers()
        };
    }

    public abstract ResourceContainerDictionary GetStartingDataContainers();
}

public abstract class SingleResourceModuleGameplay : ModuleGameplay, IDataContainer<SingleResourceModuleData>
{
    [SerializeField]
    private SingleResourceModuleData data;
    public SingleResourceModuleData Data { get { return data; } set { data = value; } }
    public override string ModuleInstanceID { get { return data.ModuleInstanceID; } }
    public override string PowerableInstanceID { get { return data.PowerableInstanceID; } }
    public override float FaultedPercentage { get { return data.FaultedPercentage; } set { data.FaultedPercentage = value; } }

    public SpriteRenderer flowAmountRenderer;

    protected Matter ResourceType { get { return this.data.Container == null ? Matter.Unspecified : this.data.Container.MatterType; } }

    public override ResourceContainer Get(Matter c)
    {
        if (Data.Container.MatterType == c)
            return data.Container;
        else
            return null;
    }

    public override bool HasContainerFor(Matter c)
    {
        return (Data.Container.MatterType == c);
    }

    protected override void OnStart()
    {
        base.OnStart();
        FlowManager.Instance.Sinks.Add(this);
    }

    public override void InitializeStartingData()
    {
        this.Data = new SingleResourceModuleData()
        {
            ModuleInstanceID = GetModuleInstanceID(),
            EnergyHistory = new LocalEnergyHistory(),
            MatterHistory = new LocalMatterHistory(),
            ModuleType = GetModuleType(),
            Container = GetStartingDataContainer()
        };
    }
    
    public abstract ResourceContainer GetStartingDataContainer();
}

public abstract class Converter : MultipleResourceModuleGameplay
{
    protected override void OnStart()
    {
        FlowManager.Instance.Converters.Add(this);
    }

    public override void Tick()
    {
        this.Convert();
    }

    public abstract void Convert();

    public override void OnAdjacentChanged()
    {
        ClearHooks();

        foreach(IPumpable m in Adjacent)
        {
            if (m is ISink)
            {
                OnSinkConnected(m as ISink);
            }
        }
    }

    public abstract void ClearHooks();
    public virtual void OnSinkConnected(ISink s) { }
}

public interface IPowerToggleable
{
    bool IsOn { get; }
    void TogglePower();
}