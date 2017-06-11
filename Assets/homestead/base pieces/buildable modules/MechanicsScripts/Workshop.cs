using RedHomestead.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using RedHomestead.Crafting;

public class Workshop : ResourcelessHabitatGameplay
{
    private Craftable _currentCraftable = Craftable.Unspecified;
    public Craftable CurrentCraftable { get { return _currentCraftable; } private set { _currentCraftable = value; } }
    public float CraftableProgress;
    public Transform[] CraftableHolograms;

    public override float WattsConsumed
    {
        get
        {
            return 0f;
        }
    }

    public override Module GetModuleType()
    {
        return Module.Workshop;
    }

    public override void OnAdjacentChanged() { }

    public override void Report() {}

    public override void Tick() { }

    protected override void OnStart()
    {
        this.SetCurrentCraftable(Craftable.Unspecified);
    }

    public void SetCurrentCraftable(Craftable c)
    {
        if (_currentCraftable != Craftable.Unspecified)
        {
            CraftableHolograms[Convert.ToInt32(_currentCraftable)].gameObject.SetActive(false);
        }

        _currentCraftable = c;

        if (_currentCraftable == Craftable.Unspecified)
        {
            CraftableHolograms[0].parent.gameObject.SetActive(false);
        }
        else
        {
            CraftableHolograms[0].parent.gameObject.SetActive(true);
            CraftableHolograms[Convert.ToInt32(_currentCraftable)].gameObject.SetActive(true);
        }
    }
}
