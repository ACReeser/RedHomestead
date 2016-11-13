/*
	SetRenderQueue.cs
 
	Sets the RenderQueue of an object's materials on Awake. This will instance
	the materials, so the script won't interfere with other renderers that
	reference the same materials.
*/

using UnityEngine;

public class SetRenderQueue : MonoBehaviour
{
    public int queue;

    void Awake()
    {
        Material[] materials = this.gameObject.GetComponent<Renderer>().materials;
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].renderQueue = queue;
        }
    }
}