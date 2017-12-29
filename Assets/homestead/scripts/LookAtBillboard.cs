using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtBillboard : LookAtCamera
{
    public float distance = 7.0f;
    Vector3 startScale;
    protected float currentDistance;

    public override void Start()
    {
        base.Start();
        startScale = transform.localScale;
    }

    public override void Update() {
        base.Update();

        currentDistance = Vector3.Distance(m_Camera.transform.position, transform.position);
        Vector3 newScale = startScale * (currentDistance / distance);
        transform.localScale = newScale;
    }
}
