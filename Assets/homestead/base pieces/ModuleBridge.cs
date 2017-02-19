using UnityEngine;
using System.Collections;

public class ModuleBridge : MonoBehaviour {
    public static ModuleBridge Instance;
    public Transform[] Modules;

    void Awake()
    {
        Instance = this;
    }
}
