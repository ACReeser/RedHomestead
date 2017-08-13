using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;
using System.Collections.Generic;
using RedHomestead.Buildings;
using System.Linq;

public class Warehouse : ResourcelessGameplay, ICrateSnapper, ITriggerSubscriber
{
    public class WarehouseResourceList
    {
        private readonly List<ResourceComponent>[] lists;
        private readonly Action<List<ResourceComponent>> onResourcesDepleted;

        public WarehouseResourceList(Action<List<ResourceComponent>> OnResourcesDepleted)
        {
            this.onResourcesDepleted = OnResourcesDepleted;
            this.lists = new List<ResourceComponent>[MatterExtensions.MaxMatter()];
        }

        public bool Contains(Matter type)
        {
            int i = Convert.ToInt32(type);
            if (lists[i] == null)
            {
                return false;
            }
            else
            {
                return lists[i].Count > 0;
            }
        }

        public void Add(ResourceComponent r)
        {
            int i = Convert.ToInt32(r.Data.Container.MatterType);
            if (lists[i] == null)
            {
                lists[i] = new List<ResourceComponent>()
                {
                    r
                };
            }
            else
            {
                lists[i].Add(r);
            }
        }

        public void Remove(ResourceComponent r)
        {
            int i = Convert.ToInt32(r.Data.Container.MatterType);
            if (lists[i] == null)
            {
                //noop
            }
            else
            {
                lists[i].Remove(r);
            }
        }

        public bool CanConsume(List<ResourceEntry> resources)
        {
            UnityEngine.Debug.Log("Checking can consume");

            foreach(ResourceEntry re in resources)
            {
                if (!CanConsume(re.Type, re.Count))
                    return false;
            }

            return true;
        }

        public bool CanConsume(Matter type, float amount)
        {
            List<ResourceComponent> list = lists[Convert.ToInt32(type)];
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

        public void Consume(List<ResourceEntry> resources)
        {
            UnityEngine.Debug.Log("Consuming resources from warehouses");

            //maintain a separate list of depleted containers so we don't mess up the iteration
            List<ResourceComponent> depleted = new List<ResourceComponent>();

            foreach(ResourceEntry re in resources)
            {
                int resourceIndex = 0;
                float amount = re.Count;
                List<ResourceComponent> list = lists[Convert.ToInt32(re.Type)];

                while (amount > 0 && resourceIndex < list.Count)
                {
                    amount -= list[resourceIndex].Data.Container.Pull(amount);
                    
                    if (list[resourceIndex].Data.Container.CurrentAmount <= 0f)
                    {
                        depleted.Add(list[resourceIndex]);
                    }

                    if (amount <= 0f)
                        GuiBridge.Instance.ShowNews(NewsSource.CraftingConsumed.CloneWithSuffix(String.Format("{0} {1}", re.Count, re.Type.ToString())));

                    resourceIndex++;
                }
            }

            //call the action if there were depleted containers
            if (depleted.Count > 0)
                this.onResourcesDepleted(depleted);
        }

        public void Consume(Matter type, float amount)
        {
            Consume(new List<ResourceEntry>()
            {
                new ResourceEntry(amount, type)
            });
        }
    }

    private class WarehouseRow
    {
        public WarehouseRow(Warehouse parent, Vector3 rowCenter)
        {
            this.parent = parent;
            this.rowCenter = rowCenter;
        }

        private const int ColumnCount = 6;
        private const int RowCount = 2;
        private const float ExtremeZCoordinate = 3f;
        private ResourceComponent[] stack = new ResourceComponent[ColumnCount * RowCount];
        private Dictionary<ResourceComponent, int> IndexMap = new Dictionary<ResourceComponent, int>();
        private Vector3 rowCenter;
        private readonly Warehouse parent;

        public void Capture(ICrateSnapper dad, ResourceComponent res)
        {
            if (CanSnap(res))
            {
                res.SnapCrate(dad, this.allocateCrate(res));
                Warehouse.GlobalResourceList.Add(res);
            }
        }

        private Vector3 allocateCrate(ResourceComponent res)
        {
            for (int i = 0; i < stack.Length; i++)
            {
                if (stack[i] == null) {
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
                ((i % ColumnCount) * (ExtremeZCoordinate * 2f  / ((float)ColumnCount - 1))) - ExtremeZCoordinate
            );
        }

        public bool Release(ResourceComponent res, out WarehouseRow row)
        {
            row = this;

            if (IndexMap.ContainsKey(res))
            {
                int index = IndexMap[res];
                IndexMap.Remove(res);
                Warehouse.GlobalResourceList.Remove(res);
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

    static Warehouse()
    {
        GlobalResourceList = new WarehouseResourceList(OnResourcesDepleted);
        Warehouses = new List<Warehouse>();
    }
    public static readonly WarehouseResourceList GlobalResourceList;
    public static readonly List<Warehouse> Warehouses;
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
}
