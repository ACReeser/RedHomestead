using UnityEngine;
using System.Collections;

public class TerrainColliderToggle : MonoBehaviour {
    
    public Collider TerrainCollider;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Physics.IgnoreCollision(other, TerrainCollider, true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            Physics.IgnoreCollision(other, TerrainCollider, false);
        }
    }
}
