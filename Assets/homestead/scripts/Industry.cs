using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RedHomestead.Industry
{
    public interface IPumpable
    {
        void OnAdjacentChanged();
        List<IPumpable> AdjacentPumpables { get; }
    }

    public interface ISink: IPumpable
    {
        ResourceContainer Get(Matter c);
        bool HasContainerFor(Matter c);
    }

    public static class IndustryExtensions
    {
        public static void AddAdjacentPumpable(IPumpable adjacentAlpha, IPumpable adjacentBeta)
        {
            adjacentAlpha.AdjacentPumpables.Add(adjacentBeta);
            adjacentBeta.AdjacentPumpables.Add(adjacentAlpha);

            adjacentAlpha.OnAdjacentChanged();
            adjacentBeta.OnAdjacentChanged();
        }

        public static void RemoveAdjacentPumpable(IPumpable adjacentAlpha, IPumpable adjacentBeta)
        {
            adjacentAlpha.AdjacentPumpables.Remove(adjacentBeta);
            adjacentBeta.AdjacentPumpables.Remove(adjacentAlpha);

            adjacentAlpha.OnAdjacentChanged();
            adjacentBeta.OnAdjacentChanged();
        }
    }
}
