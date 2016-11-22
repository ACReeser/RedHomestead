using UnityEngine;
using System.Collections;

public class AnimatedTexture : MonoBehaviour {
    public float speed = 1f;
    public Vector2 direction = Vector2.right;
    public bool animateDiffuse = true, animateEmissive = false;
    public string diffuseName = "_MainTex", emissiveName = "_EmissionMap";

    private Material animatedMaterial;

    
	// Use this for initialization
	void Start () {
        this.animatedMaterial = this.GetComponent<Renderer>().material;
	}
	
	// Update is called once per frame
	void Update ()
    {
        CheckMoveTexture(animateDiffuse, diffuseName);

        CheckMoveTexture(animateEmissive, emissiveName);
    }

    private void CheckMoveTexture(bool doAnimate, string textureName)
    {
        if (doAnimate)
            this.animatedMaterial.SetTextureOffset(textureName, this.animatedMaterial.GetTextureOffset(textureName) + (direction * speed * Time.deltaTime));
    }
}
