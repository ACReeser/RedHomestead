﻿using RedHomestead.Buildings;
using RedHomestead.Electricity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHabitatModule: IBuildable, IPowerable
{
    Habitat LinkedHabitat { get; set; }
    List<IHabitatModule> AdjacentModules { get; set; }
    Transform[] Bulkheads { get; }
}

public static class HabitatModuleExtensions
{
    public static void AssignConnections(IHabitatModule alpha, IHabitatModule beta)
    {
        bool alphaHasHab = alpha.LinkedHabitat != null;
        bool betaHasHab = beta.LinkedHabitat != null;

        if (!alphaHasHab && betaHasHab)
            alpha.SetHabitat(beta.LinkedHabitat);
        else if (alphaHasHab && !betaHasHab)
            beta.SetHabitat(alpha.LinkedHabitat);

        if (alpha.AdjacentModules == null)
            alpha.AdjacentModules = new List<IHabitatModule>();

        if (beta.AdjacentModules == null)
            beta.AdjacentModules = new List<IHabitatModule>();

        alpha.AdjacentModules.Add(beta);
        beta.AdjacentModules.Add(alpha);
    }

    public static void RemoveConnection(IHabitatModule alpha, IHabitatModule beta)
    {
#warning does not reset linked habitat state
        alpha.AdjacentModules.Remove(beta);
        beta.AdjacentModules.Remove(alpha);
    }

    public static void SetHabitat(this IHabitatModule self, Habitat habitat)
    {
        self.LinkedHabitat = habitat;

        if (self is Converter)
            (self as Converter).OnSinkConnected(habitat);

        if (self.AdjacentModules != null)
        {
            foreach (IHabitatModule sibling in self.AdjacentModules)
            {
                if (sibling.LinkedHabitat == null)
                {
                    sibling.SetHabitat(habitat);
                }
            }
        }
    }

    public static int GetBulkheadIndex(this IHabitatModule self, Transform t)
    {
        if (self.Bulkheads == null)
        {
            UnityEngine.Debug.LogWarning("Looking for bulkhead when no bulkheads array is extant!");
            return -1;
        }

        for (int i = 0; i < self.Bulkheads.Length; i++)
        {
            if (self.Bulkheads[i] == t)
                return i;
        }
        return -1;
    }
}

public abstract class GenericBaseModule : ResourcelessHabitatGameplay
{
    public override float WattsConsumed
    {
        get
        {
            return 0;
        }
    }

    public override void OnAdjacentChanged()
    {
    }

    public override void Report()
    {
    }

    public override void Tick()
    {
    }
}

