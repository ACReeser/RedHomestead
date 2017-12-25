using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;
using System.Collections.Generic;
using RedHomestead.Buildings;
using System.Linq;

public interface IWarehouse
{
    void AddToGlobalList(ResourceComponent res);
    void RemoveFromGlobalList(ResourceComponent res);
}

public class ResourceRow<T>
    where T: class, IWarehouse
{
    protected readonly int ColumnCount = 6;
    protected readonly int RowCount = 2;
    protected readonly float ExtremeZCoordinate = 3f;
    protected readonly ResourceComponent[] stack;
    protected Dictionary<ResourceComponent, int> IndexMap = new Dictionary<ResourceComponent, int>();
    protected readonly Vector3 rowCenter;
    protected readonly T parent;

    public ResourceRow(T parent, Vector3 rowCenter)
    {
        this.parent = parent;
        this.rowCenter = rowCenter;
        stack =  new ResourceComponent[ColumnCount * RowCount];
    }

    public void Capture(ICrateSnapper dad, ResourceComponent res)
    {
        if (CanSnap(res))
        {
            res.SnapCrate(dad, this.allocateCrate(res));
            parent.AddToGlobalList(res);
        }
    }

    private Vector3 allocateCrate(ResourceComponent res)
    {
        for (int i = 0; i < stack.Length; i++)
        {
            if (stack[i] == null)
            {
                IndexMap[res] = i;
                stack[i] = res;
                return GetVectorFromIndex(i);
            }
        }

        UnityEngine.Debug.LogError("Cannot find a place for crate to snap to in warehouse");

        return Vector3.zero;
    }

    private Vector3 GetVectorFromIndex(int i)
    {
        return this.rowCenter + new Vector3(0,
            i < ColumnCount ? 0f : 1f,
            ((i % ColumnCount) * (ExtremeZCoordinate * 2f / ((float)ColumnCount - 1))) - ExtremeZCoordinate
        );
    }

    protected bool _Release(ResourceComponent res, out ResourceRow<T> row)
    {
        row = this;

        if (IndexMap.ContainsKey(res))
        {
            int index = IndexMap[res];
            IndexMap.Remove(res);
            parent.RemoveFromGlobalList(res);
            stack[index] = null;
            return true;
        }
        else
        {
            return false;
        }
    }

    public bool CanSnap(ResourceComponent res)
    {
        return IndexMap.Keys.Count < stack.Length &&
            !IndexMap.ContainsKey(res); //don't allow things that are already snapped
    }
}

public class ResourceList
{
    private readonly List<ResourceComponent>[] MatterIndexedResourceLists;
    private readonly Action<List<ResourceComponent>> onResourcesDepleted;
    private float _CurrentAmount;

    public ResourceList(Action<List<ResourceComponent>> OnResourcesDepleted)
    {
        this.onResourcesDepleted = OnResourcesDepleted;
        this.MatterIndexedResourceLists = new List<ResourceComponent>[MatterExtensions.MaxMatter()];
    }

    public bool Contains(Matter type)
    {
        int i = type.Index();
        if (MatterIndexedResourceLists[i] == null)
        {
            return false;
        }
        else
        {
            return MatterIndexedResourceLists[i].Count > 0;
        }
    }

    public void Add(ResourceComponent r)
    {
        int i = r.Data.Container.MatterType.Index();
        _CurrentAmount += r.Data.Container.CurrentAmount;

        if (MatterIndexedResourceLists[i] == null)
        {
            MatterIndexedResourceLists[i] = new List<ResourceComponent>()
                {
                    r
                };
        }
        else
        {
            MatterIndexedResourceLists[i].Add(r);
        }
    }

    public void Remove(ResourceComponent r)
    {
        _CurrentAmount -= r.Data.Container.CurrentAmount;
        int i = r.Data.Container.MatterType.Index();
        if (MatterIndexedResourceLists[i] == null)
        {
            //noop
        }
        else
        {
            MatterIndexedResourceLists[i].Remove(r);
        }
    }

    public bool CanConsume(List<IResourceEntry> resources)
    {
        UnityEngine.Debug.Log("Checking can consume");

        foreach (IResourceEntry re in resources)
        {
            if (!CanConsume(re.Type, re.AmountByVolume))
                return false;
        }

        return true;
    }

    public bool CanConsume(Matter type, float amount)
    {
        List<ResourceComponent> list = MatterIndexedResourceLists[Convert.ToInt32(type)];
        if (list == null)
        {
            return false;
        }
        else
        {
            //validate there is enough matter
            if (list.Sum(r => r.Data.Container.CurrentAmount) < amount)
                return false;
            else
                return true;
        }
    }

    public void Consume(List<IResourceEntry> resources, bool isCrafting = true)
    {
        UnityEngine.Debug.Log("Consuming resources from warehouses");

        //maintain a separate list of depleted containers so we don't mess up the iteration
        List<ResourceComponent> depleted = new List<ResourceComponent>();

        foreach (IResourceEntry re in resources)
        {
            int resourceIndex = 0;
            float amount = re.AmountByVolume;
            int i = re.Type.Index();
            List<ResourceComponent> list = MatterIndexedResourceLists[i];

            _CurrentAmount -= re.AmountByVolume;

            while (amount > 0 && resourceIndex < list.Count)
            {
                amount -= list[resourceIndex].Data.Container.Pull(amount);

                if (list[resourceIndex].Data.Container.CurrentAmount <= 0f)
                {
                    depleted.Add(list[resourceIndex]);
                }

                if (isCrafting && amount <= 0f)
                    GuiBridge.Instance.ShowNews(NewsSource.CraftingConsumed.CloneWithSuffix(String.Format("{0} {1}", re.AmountByVolume, re.Type.ToString())));

                resourceIndex++;
            }
        }

        //call the action if there were depleted containers
        if (depleted.Count > 0)
            this.onResourcesDepleted(depleted);
    }

    public void Consume(Matter type, float amount, bool isCrafting = false)
    {
        Consume(new List<IResourceEntry>()
        {
            new ResourceVolumeEntry(amount, type)
        }, isCrafting);
    }

    public float CurrentAmount()
    {
        return this._CurrentAmount;
    }

    public float CurrentAmount(Matter type)
    {
        int i = type.Index();
        return this.MatterIndexedResourceLists[i].Sum(x => x.Data.Container.CurrentAmount);
    }

    public int[] GetNonEmptyMatterSlots()
    {
        int max = MatterExtensions.MaxMatter();
        List<int> slots = new List<int>();
        for (int i = 0; i < max; i++)
        {
            if (MatterIndexedResourceLists[i] != null && MatterIndexedResourceLists[i].Sum(x => x.Data.Container.CurrentAmount) > 0f)
            {
                slots.Add(i);
            }
        }
        return slots.ToArray();
    }
}

public class Warehouse : ResourcelessGameplay, ICrateSnapper, ITriggerSubscriber, IWarehouse
{
    public class WarehouseRow: ResourceRow<Warehouse>
    {
        public WarehouseRow(Warehouse parent, Vector3 rowCenter): base(parent, rowCenter)
        {
        }

        public bool Release(ResourceComponent res, out WarehouseRow row)
        {
            ResourceRow<Warehouse> fromParent = null;
            bool released = _Release(res, out fromParent);
            row = fromParent as WarehouseRow;
            return released;
        }
    }

    static Warehouse()
    {
        GlobalResourceList = new ResourceList(OnResourcesDepleted);
        Warehouses = new List<Warehouse>();
    }
    public static readonly List<Warehouse> Warehouses;
    public static readonly ResourceList GlobalResourceList;
    public static void OnResourcesDepleted(List<ResourceComponent> resources)
    {
        foreach (ResourceComponent res in resources)
        {
            foreach(Warehouse w in Warehouses)
            {
                w.DetachCrate(res);
            }
            GameObject.Destroy(res.gameObject);
        }
    }

    private WarehouseRow left, middle, right;
    private Dictionary<WarehouseRow, Coroutine> CrateInterferenceTimers = new Dictionary<WarehouseRow, Coroutine>();
    private const float SnapInterferenceTimerSeconds = 1.25f;

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    // Use this for initialization
    protected override void OnStart() {
        Warehouses.Add(this);
        left = new WarehouseRow(this, transform.GetChild(0).position);
        middle = new WarehouseRow(this, transform.GetChild(1).position);
        right = new WarehouseRow(this, transform.GetChild(2).position);
    }

    void OnDestroy()
    {
        Warehouses.Remove(this);
    }


    public void OnChildTriggerEnter(TriggerForwarder child, Collider other, IMovableSnappable res)
    {
        WarehouseRow whichRow = GetRow(child.name);
        if (whichRow != null && res is ResourceComponent && !CrateInterferenceTimers.ContainsKey(whichRow))
        {
            whichRow.Capture(this, res as ResourceComponent);
        }
    }

    private WarehouseRow GetRow(string childName)
    {
        switch (childName)
        {
            case "leftRow":
                return left;
            case "rightRow":
                return right;
            case "centerRow":
                return middle;
        }

        return null;
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        WarehouseRow wasAttachedTo = null;
        //we don't know which row we are detaching from! so...
        //we have to search them all possibly
        //short circuit hack = if one of them returns true, the remaining calls will not be made
        if(this.left.Release(detaching as ResourceComponent, out wasAttachedTo) || 
           this.middle.Release(detaching as ResourceComponent, out wasAttachedTo) || 
           this.right.Release(detaching as ResourceComponent, out wasAttachedTo))
        {
            this.CrateInterferenceTimers.Add(wasAttachedTo, StartCoroutine(CrateInterferenceCountdown(wasAttachedTo)));
        }
    }

    private IEnumerator CrateInterferenceCountdown(WarehouseRow row)
    {
        yield return new WaitForSeconds(SnapInterferenceTimerSeconds);

        this.CrateInterferenceTimers.Remove(row);
    }

    public override void OnAdjacentChanged() { }

    public override void Tick() { }

    public override void Report() { }

    public override void InitializeStartingData() {
        this.Data = new ResourcelessModuleData()
        {
            ModuleInstanceID = Guid.NewGuid().ToString(),
            ModuleType = GetModuleType()
        };
    }

    public override Module GetModuleType()
    {
        return Module.Warehouse;
    }

    public void AddToGlobalList(ResourceComponent res)
    {
        Warehouse.GlobalResourceList.Add(res);
    }

    public void RemoveFromGlobalList(ResourceComponent res)
    {
        Warehouse.GlobalResourceList.Remove(res);
    }
}
