using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MarsRegionMapLabeller : MonoBehaviour {
    public Font font;
    public Material fontMat;
    public float HideFontThreshold = 3.1f;

    private Transform[] rootRegions;
    private TextMesh[] textRegions;


    // Use this for initialization
    void Start () {
        List<Transform> children = new List<Transform>();
        List<TextMesh> grandchildTexts = new List<TextMesh>();

        foreach(Transform t in this.transform)
        {
            if (t.gameObject.activeInHierarchy && t.name != "equator" && t.name != "prime_meridian")
            {
                children.Add(t);
                GameObject g = new GameObject();
                TextMesh newT = g.AddComponent<TextMesh>();
                g.transform.SetParent(t);
                g.transform.localPosition = Vector3.zero;
                g.transform.Translate((this.transform.position - t.position).normalized * -.15f, Space.Self);
                grandchildTexts.Add(newT);
                newT.text = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(t.name.Replace('_', ' '));
                newT.characterSize = .02f;
                newT.fontSize = 28;
                newT.alignment = TextAlignment.Center;
                newT.anchor = TextAnchor.MiddleCenter;
                newT.font = font;
                newT.GetComponent<MeshRenderer>().material = fontMat;
                g.transform.localRotation = Quaternion.identity;
            }
        }

        rootRegions = children.ToArray();
        textRegions = grandchildTexts.ToArray();
	}
	
	// Update is called once per frame
	void Update () {
		foreach(TextMesh t in this.textRegions)
        {
            t.transform.LookAt(Camera.main.transform);
            t.transform.Rotate(Vector3.up, 180);

            float dist = Vector3.Distance(t.transform.position, Camera.main.transform.position);

            if (dist > HideFontThreshold)
            {
                t.color = new Color(1, 1, 1, t.color.a - .02f);
            }
            else
            {
                t.color = new Color(1, 1, 1, t.color.a + .02f);
            }            
        }
	}
}
