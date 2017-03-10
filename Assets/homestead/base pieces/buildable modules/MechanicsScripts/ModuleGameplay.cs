using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RedHomestead.Simulation;
using RedHomestead.Persistence;

[Serializable]
public abstract class PoweredModuleData : FacingData
{ 
    public LocalEnergyHistory EnergyHistory = new LocalEnergyHistory();
    public RedHomestead.Buildings.Module ModuleType;
}

[Serializable]
public abstract class ModuleData: PoweredModuleData
{
    public LocalMatterHistory MatterHistory = new LocalMatterHistory();
}

[Serializable]
public class ResourceContainerDictionary: SerializableDictionary<Matter, ResourceContainer> { }

[Serializable]
public class MultipleResourceModuleData: ModuleData
{
    public ResourceContainerDictionary Containers;
}

[Serializable]
public class SingleResourceModuleData : ModuleData
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
        this.OnStart();
    }

    protected virtual void OnStart() { }
    public virtual void OnPowerChanged() { }
    public abstract void OnAdjacentChanged();
    public abstract void Tick();
    public abstract void Report();
    public abstract ResourceContainer Get(Matter c);
    public abstract bool HasContainerFor(Matter c);
    public abstract void InitializeStartingData();
    public abstract RedHomestead.Buildings.Module GetModuleType();
}

public abstract class ResourcelessGameplay : ModuleGameplay, IDataContainer<PoweredModuleData>
{
    private PoweredModuleData data;
    public PoweredModuleData Data { get { return data; } set { data = value; } }

    public override ResourceContainer Get(Matter c)
    {
        return null;
    }

    public override bool HasContainerFor(Matter c)
    {
        return false;
    }
}

public abstract class PowerSupply : ResourcelessGameplay
{
    public abstract float WattsPerTick { get; }
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
public class ResourceContainer
{
    public ResourceContainer() { }
    public ResourceContainer(float initialAmount)
    {
        this.Amount = initialAmount;
    }

    public Matter MatterType;

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

public interface IPowerToggleable
{
    bool IsOn { get; }
    void TogglePower();
}