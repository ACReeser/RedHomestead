using UnityEngine;
using System.Collections;

public class ModuleBridge : MonoBehaviour {
    public static ModuleBridge Instance;
    public Transform[] Modules;
    public Transform ConstructionZonePrefab, IceDrillPrefab, PowerCubePrefab, WaterDepositPrefab, MobileSolarPanelPrefab;

    void Awake()
    {
        Instance = this;
    }
}
