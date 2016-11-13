using UnityEngine;
using System.Collections;

public class TerrainColliderToggle : MonoBehaviour {
    
    public Collider TerrainCollider;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "FPSController")
        {
            Physics.IgnoreCollision(other, TerrainCollider, true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.name == "FPSController")
        {
            Physics.IgnoreCollision(other, TerrainCollider, false);
        }
    }
}
