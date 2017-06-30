using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconAtlas : MonoBehaviour {
    public static IconAtlas Instance { get; private set; }

    public Sprite[] ResourceIcons, CompoundIcons, MiscIcons, CraftableIcons;

    void Awake()
    {
        Instance = this;
    }
}
