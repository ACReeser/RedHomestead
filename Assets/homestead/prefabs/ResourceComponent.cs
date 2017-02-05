using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;

public class ResourceComponent : MonoBehaviour {
    public Matter ResourceType;
    public int Quantity = 1;
    //todo: add negation getter
    //todo: could be CurrentConstructionZone reference instead
    internal bool IsInConstructionZone = false;
}
