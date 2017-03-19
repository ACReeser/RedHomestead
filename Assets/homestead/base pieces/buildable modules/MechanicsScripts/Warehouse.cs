using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;
using System.Collections.Generic;
using RedHomestead.Buildings;

public class Warehouse : ResourcelessGameplay, ICrateSnapper, ITriggerSubscriber
{
    private class WarehouseRow
    {
        public WarehouseRow(Vector3 rowCenter)
        {
            this.rowCenter = rowCenter;
        }

        private const int ColumnCount = 6;
        private const int RowCount = 2;
        private const float ExtremeZCoordinate = 3f;
        private ResourceComponent[] stack = new ResourceComponent[ColumnCount * RowCount];
        private Dictionary<ResourceComponent, int> IndexMap = new Dictionary<ResourceComponent, int>();
        private Vector3 rowCenter;

        public void Capture(ICrateSnapper dad, ResourceComponent res)
        {
            if (CanSnap(res))
            {
                res.SnapCrate(dad, this.allocateCrate(res));
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


    private WarehouseRow left, middle, right;
    private Dictionary<WarehouseRow, Coroutine> CrateInterferenceTimers = new Dictionary<WarehouseRow, Coroutine>();
    private const float SnapInterferenceTimerSeconds = 1.25f;

    public override float WattRequirementsPerTick
    {
        get
        {
            return 0f;
        }
    }

    // Use this for initialization
    protected override void OnStart() {
        left = new WarehouseRow(transform.GetChild(0).position);
        middle = new WarehouseRow(transform.GetChild(1).position);
        right = new WarehouseRow(transform.GetChild(2).position);
    }

    // Update is called once per frame
    //void Update() {

    //}


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
