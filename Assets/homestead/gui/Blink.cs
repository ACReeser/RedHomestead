using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class Blink : MonoBehaviour {
    public float offset = 0f;

    private Image img;
    private Color startColor;
    private Color endColor;
    // Use this for initialization
    void Start () {
        this.img = GetComponent<Image>();
        startColor = img.color;
        endColor = new Color(img.color.r, img.color.g, img.color.b, 0);
	}
	
	// Update is called once per frame
	void Update () {
        //float sin = Mathf.Sin(Mathf.PI * 2f * ((Time.fixedUnscaledTime + offset) % 1));
        float f = Mathfx.Hermite(0, 1, ((Time.fixedUnscaledTime + offset) % 1));
        img.color = Color.Lerp(startColor, endColor, f);		
	}
}
