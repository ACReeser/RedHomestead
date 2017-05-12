using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HabitatModuleBase : MonoBehaviour, IHabitatModule
{
    [HideInInspector]
    public List<IHabitatModule> AdjacentModules { get; private set; }
    [HideInInspector]
    public Habitat LinkedHabitat { get; private set; }

    public void AddAdjacent(IHabitatModule adjacent)
    {
        if (this.LinkedHabitat == null && adjacent.LinkedHabitat != null)
            this.SetHabitat(adjacent.LinkedHabitat);

        AdjacentModules.Add(adjacent);
    }

    public void InitializeStartingData()
    {
    }

    public void SetHabitat(Habitat habitat)
    {
        this.LinkedHabitat = habitat;

        foreach (IHabitatModule sibling in AdjacentModules)
        {
            if (sibling.LinkedHabitat == null)
            {
                sibling.SetHabitat(habitat);
            }
        }
    }


    void Awake()
    {
        this.AdjacentModules = new List<IHabitatModule>();
    }
}