using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;

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
    public string PowerGridInstanceID;
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

public abstract class ModuleGameplay : MonoBehaviour, ISink
{
    public SpriteRenderer PowerIndicator;
    protected AudioSource SoundSource;

    private bool _hasPower;
    public bool HasPower {
        get { return _hasPower; }
        set {
            _hasPower = value;
            if (PowerIndicator != null) {
                PowerIndicator.enabled = !_hasPower;
            }
        }
    }
    
    public abstract float WattRequirementsPerTick { get; }
    public abstract string ModuleInstanceID { get; }
    public abstract string PowerGridInstanceID { get; set; }

    protected List<ModuleGameplay> Adjacent = new List<ModuleGameplay>();
    
    public void LinkToModule(ModuleGameplay adjacent)
    {
        Adjacent.Add(adjacent);
        OnAdjacentChanged();
    }

    public void UnlinkFromModule(ModuleGameplay adjacent)
    {
        Adjacent.Remove(adjacent);
        OnAdjacentChanged();
    }

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
    public override string PowerGridInstanceID { get { return data.PowerGridInstanceID; } set { data.PowerGridInstanceID = value; } }

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


public interface ISink
{
    ResourceContainer Get(Matter c);
    bool HasContainerFor(Matter c);
}

public abstract class MultipleResourceModuleGameplay: ModuleGameplay, IDataContainer<MultipleResourceModuleData>
{
    [SerializeField]
    private MultipleResourceModuleData data;
    public MultipleResourceModuleData Data { get { return data; } set { data = value; } }
    public override string ModuleInstanceID { get { return data.ModuleInstanceID; } }
    public override string PowerGridInstanceID { get { return data.PowerGridInstanceID; } set { data.PowerGridInstanceID = value; } }

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
    public override string PowerGridInstanceID { get { return data.PowerGridInstanceID; } set { data.PowerGridInstanceID = value; } }

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

        foreach(ModuleGameplay m in Adjacent)
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

[Serializable]
public class Container
{
    public Container() { }
    public Container(float initialAmount)
    {
        this.Amount = initialAmount;
    }

    //Serializable
    public float TotalCapacity = 1f;
    [SerializeField]
    protected float Amount;

    public float CurrentAmount { get { return Amount; } }

    public float UtilizationPercentage
    {
        get
        {
            if (TotalCapacity <= 0)
                return 0;

            return Amount / TotalCapacity;
        }
    }

    public string UtilizationPercentageString()
    {
        return (int)(UtilizationPercentage * 100) + "%";
    }

    public float AvailableCapacity
    {
        get
        {
            return TotalCapacity - Amount;
        }
    }

    public float Push(float pushAmount)
    {
        if (AvailableCapacity > 0)
        {
            Amount += pushAmount;

            if (Amount > TotalCapacity)
            {
                float overage = Amount - TotalCapacity;

                Amount = TotalCapacity;

                return overage;
            }
            else
            {
                return 0;
            }
        }
        else
        {
            return pushAmount;
        }
    }

    public float Pull(float pullAmount)
    {
        if (Amount <= 0)
        {
            return 0;
        }
        else
        {
            if (Amount > pullAmount)
            {
                Amount -= pullAmount;

                return pullAmount;
            }
            else
            {
                float allToGive = Amount;

                Amount = 0;

                return allToGive;
            }
        }
    }

}

[Serializable]
public class ResourceContainer: Container
{
    public ResourceContainer() { }
    public ResourceContainer(float initialAmount) : base(initialAmount) { }

    //Serializable
    public Matter MatterType;
}

[Serializable]
public class EnergyContainer : Container
{
    public EnergyContainer() { }
    public EnergyContainer(float initialAmount) : base(initialAmount) { }

    //Serializable
    public Energy EnergyType;
}

public interface IPowerToggleable
{
    bool IsOn { get; }
    void TogglePower();
}