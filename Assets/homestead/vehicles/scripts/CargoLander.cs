using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CargoLander : MonoBehaviour {
    private const int MinimumAltitude = 300;
    private const float AltitudeRangeAboveMinimum = 100f;
    private const float LandingAltitudeAboveLandingZone = 2.25f;
    private const float LandingTimeSeconds = 20f;
    private const float LandingStageTimeSeconds = LandingTimeSeconds / 2f;

    public Transform LanderPivot, LanderHinge, Lander;
    public LandingZone LZ;
    public ParticleSystem RocketFire, RocketSmoke;


    public static CargoLander Instance;

	// Use this for initialization
	void Start () {
        Instance = this;
        Lander.gameObject.SetActive(false);
	}

    // Update is called once per frame
    //void Update () {

    //}
    private Coroutine landing;
    public void Land()
    {
        if (landing != null)
            StopCoroutine(landing);

        GuiBridge.Instance.ShowNews(NewsSource.IncomingCargo);
        Lander.gameObject.SetActive(true);
        float minAltitude = LZ.transform.position.y + MinimumAltitude;
        float startAltitude = UnityEngine.Random.Range(minAltitude, minAltitude+ AltitudeRangeAboveMinimum);
        float randomAngle = UnityEngine.Random.Range(0, 360);
        LanderHinge.localPosition = new Vector3(startAltitude, 0, 0);
        LanderHinge.localRotation = Quaternion.identity;
        LanderPivot.localRotation = Quaternion.Euler(0f, randomAngle, 0f);
        Lander.localPosition = startLander = new Vector3((float)(startAltitude * Math.Sqrt(2f)), startAltitude, 0);
        endLander = new Vector3(0, startAltitude, 0f);
        
        landing = StartCoroutine(DoLand());
    }

    private Vector3 startLander, endLander;
    private Vector3 startHinge = Vector3.zero;
    private Vector3 endHinge = new Vector3(0, 0, 90f);

    private IEnumerator DoLand()
    {
        float time = 0f;

        while (time < LandingStageTimeSeconds)
        {
            Lander.localPosition = Vector3.Lerp(startLander, endLander, time / LandingStageTimeSeconds);
            yield return null;
            time += Time.deltaTime;
        }
        time = 0f;

        RocketFire.Play();
        RocketSmoke.Play();
        while (time < LandingStageTimeSeconds)
        {
            LanderHinge.localRotation = Quaternion.Euler(
                Mathfx.Sinerp(startHinge, endHinge, time / LandingStageTimeSeconds)
                //Vector3.Lerp(startHinge, endHinge, time / LandingTimeSeconds)
                );
            yield return null;
            time += Time.deltaTime;
        }
        LanderHinge.localRotation = Quaternion.Euler(endHinge);
        RocketFire.Stop();
        RocketSmoke.Stop();
    }
}
