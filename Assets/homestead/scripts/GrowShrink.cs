using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrowShrink : MonoBehaviour {
    public float GrowPercentage = .25f;
    public float ShrinkPercentage = .25f;
    public float PeriodSeconds = 2f;

    private float currentTime = 0f;
    private float halfPeriodSeconds;

    private Vector3 normalScale, maxScale, minScale;
    void Start()
    {
        halfPeriodSeconds = PeriodSeconds / 2f;
        normalScale = this.transform.localScale;
        maxScale = this.transform.localScale + this.transform.localScale * GrowPercentage;
        minScale = this.transform.localScale + this.transform.localScale * ShrinkPercentage;
    }

	// Update is called once per frame
	void Update () {
        if (currentTime <= PeriodSeconds / 2f)
        {
            this.transform.localScale = Vector3.Lerp(normalScale, maxScale, Mathf.Sin(Mathf.Lerp(0, Mathf.PI, currentTime / halfPeriodSeconds)));
            currentTime += Time.deltaTime;
        }
        else if (currentTime <= PeriodSeconds)
        {
            this.transform.localScale = Vector3.Lerp(normalScale, minScale, Mathf.Sin(Mathf.Lerp(0, Mathf.PI, (currentTime - halfPeriodSeconds) / halfPeriodSeconds)));
            currentTime += Time.deltaTime;
        }
        else if (currentTime > PeriodSeconds)
        {
            this.transform.localScale = this.transform.localScale;
            currentTime = 0f;
        }
	}
}
