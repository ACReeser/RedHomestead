using UnityEngine;
using System.Collections;
using RedHomestead.Construction;

public class ResourceComponent : MonoBehaviour {
    public Resource ResourceType;
    public int Quantity = 1;
    //todo: add negation getter
    //todo: could be CurrentConstructionZone reference instead
    internal bool IsInConstructionZone = false;
}
