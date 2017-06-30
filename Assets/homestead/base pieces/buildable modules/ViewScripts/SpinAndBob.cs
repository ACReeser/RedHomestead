using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpinAndBob : Spin {

    public float DeltaY = 1f;
    public float BobSpeed = .1f;

    private const float TWO_PI = Mathf.PI * 2f;
    private float theta = 0f;
    protected override void OnUpdate()
    {
        base.OnUpdate();

        transform.Translate((Vector3.up * DeltaY) * Mathf.Sin(theta));
        theta += (BobSpeed * Time.deltaTime) % TWO_PI;
    }
}
