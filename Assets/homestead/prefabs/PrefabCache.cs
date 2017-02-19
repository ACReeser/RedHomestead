using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using RedHomestead.Buildings;
using RedHomestead.Interiors;
using System;

public class PrefabCache<T> where T : IConvertible {

    public static Material TranslucentPlanningMat;

    private static PrefabCache<T> _cache;
    public static PrefabCache<T> Cache
    {
        get
        {
            if (_cache == null)
                _cache = new PrefabCache<T>();

            return _cache;
        }
    }

    private Dictionary<T, Transform> TransformCache = new Dictionary<T, Transform>();

    public Transform Get(T key)
    {
        Transform result = null;

        if (TransformCache.ContainsKey(key))
        {
            result = TransformCache[key];
            result.gameObject.SetActive(true);
        }
        else
        {
            result = GameObject.Instantiate<Transform>(GetPrefab(key));
            TransformCache[key] = result;
            RecurseDisableColliderSetTranslucentRenderer(result);
        }

        return result;
    }



    private void RecurseDisableColliderSetTranslucentRenderer(Transform parent)
    {
        foreach (Transform child in parent)
        {
            //only default layer
            if (child.gameObject.layer == 0)
            {
                Collider c = child.GetComponent<Collider>();
                if (c != null)
                    c.enabled = false;

                Renderer r = child.GetComponent<Renderer>();
                if (r != null)
                {
                    if (r.materials != null && r.materials.Length > 1)
                    {
                        var newMats = new Material[r.materials.Length];
                        for (int i = 0; i < r.materials.Length; i++)
                        {
                            newMats[i] = TranslucentPlanningMat;
                        }
                        r.materials = newMats;
                    }
                    else
                    {
                        r.material = TranslucentPlanningMat;
                    }
                }
            }

            RecurseDisableColliderSetTranslucentRenderer(child);
        }
    }


    public Transform GetPrefab(T key)
    {
        if (typeof(T) == typeof(Module))
        {
            return ModuleBridge.Instance.Modules[GetIndex(key)];
        }
        if (typeof(T) == typeof(Stuff))
        {
            return FloorplanBridge.Instance.StuffFields.Prefabs[GetIndex(key)];
        }
        else
            return null;
    }

    private int GetIndex(T key)
    {
        Enum myEnum = Enum.Parse(typeof(T), key.ToString()) as Enum;
        return Convert.ToInt32(myEnum);
    }
}
