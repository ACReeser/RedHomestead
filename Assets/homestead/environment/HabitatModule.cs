using UnityEngine;
using System.Collections;

public abstract class HabitatModule : MonoBehaviour {
    public Habitat LinkedHab;

    // Use this for initialization
    void Start () {
        if (LinkedHab == null)
            LinkedHab = transform.root.GetComponent<Habitat>();

        if (LinkedHab == null)
        {
            UnityEngine.Debug.LogWarning("Hab resource interface not linked!");
            this.enabled = false;
        }

        OnStart();
    }

    protected abstract void OnStart();
}
