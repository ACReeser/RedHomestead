using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtBillboardScience : LookAtBillboard {
    public static List<LookAtBillboardScience> Instances = new List<LookAtBillboardScience>();
    public static void SetInstancesEnabled(bool state)
    {
        foreach(var instance in Instances)
        {
            if (instance != null)
                instance.enabled = state;
        }
    }

    private TextMesh text;
    private string originalText;

    public override void Start()
    {
        base.Start();
        this.text = GetComponent<TextMesh>();
        this.originalText = text.text;
        Instances.Add(this);
    }

    public override void Update() {
        base.Update();
        this.text.text = this.originalText + '\n' + string.Format("{0:0.0}", currentDistance) + " meters";
	}

    void OnDestroy()
    {
        Instances.Remove(this);
    }
}
