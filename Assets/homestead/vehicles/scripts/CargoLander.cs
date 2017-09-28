using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoLander : MonoBehaviour {
    private const int MinimumAltitude = 200;
    private const float AltitudeRangeAboveMinimum = 100f;
    private const float LandingAltitudeAboveLandingZone = 2.25f;
    private const float LandingTimeSeconds = 15f;

    public Transform LanderPivot, LanderHinge, Lander;
    public LandingZone LZ;
    public ParticleSystem RocketFire;


    public static CargoLander Instance;

	// Use this for initialization
	void Start () {
        Instance = this;
        Lander.gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	//void Update () {
		
	//}

    public void Land()
    {
        GuiBridge.Instance.ShowNews(NewsSource.IncomingCargo);
        Lander.gameObject.SetActive(true);
        float minAltitude = LZ.transform.position.y + MinimumAltitude;
        float startAltitude = UnityEngine.Random.Range(minAltitude, minAltitude+ AltitudeRangeAboveMinimum);
        float randomAngle = UnityEngine.Random.Range(0, 360);
        LanderHinge.localPosition = new Vector3(startAltitude, 0, 0);
        LanderPivot.localRotation = Quaternion.Euler(0f, randomAngle, 0f);
        Lander.localPosition = new Vector3(0, startAltitude, 0);
        
        StartCoroutine(DoLand());
    }

    private Vector3 startHinge = Vector3.zero;
    private Vector3 endHinge = new Vector3(0, 0, 90f);

    private IEnumerator DoLand()
    {
        float time = 0f;
        RocketFire.Play();
        while (time < LandingTimeSeconds)
        {
            LanderHinge.localRotation = Quaternion.Euler(
                Mathfx.Hermite(startHinge, endHinge, time / LandingTimeSeconds)
                //Vector3.Lerp(startHinge, endHinge, time / LandingTimeSeconds)
                );
            yield return null;
            time += Time.deltaTime;
        }
        LanderHinge.localRotation = Quaternion.Euler(endHinge);
        RocketFire.Stop();
    }
}
