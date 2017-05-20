using RedHomestead.Buildings;
using RedHomestead.Electricity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IHabitatModule: IBuildable, IPowerable
{
    Habitat LinkedHabitat { get; set; }
    List<IHabitatModule> AdjacentModules { get; set; }
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
}


