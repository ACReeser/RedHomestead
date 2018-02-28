using UnityEngine;
using System.Collections;

public class ModuleBridge : MonoBehaviour {
    public static ModuleBridge Instance;
    public Transform[] Modules;
    public Transform ConstructionZonePrefab, IceDrillPrefab, PowerCubePrefab, WaterDepositPrefab, MobileSolarPanelPrefab, PumpPrefab, ToolboxPrefab;
    public Transform MinilabPrefab;
    public Mesh CabinetOn, CabinetOff;

    public Transform LandingZonePrefab;

    void Awake()
    {
        Instance = this;
    }
}
