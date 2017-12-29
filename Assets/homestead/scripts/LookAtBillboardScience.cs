using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtBillboardScience : LookAtBillboard {
    private TextMesh text;
    private string originalText;

    public override void Start()
    {
        base.Start();
        this.text = GetComponent<TextMesh>();
        this.originalText = text.text;
    }

    public override void Update() {
        base.Update();
        this.text.text = this.originalText + '\n' + string.Format("{0:0.0}", currentDistance) + " meters";
	}
}
