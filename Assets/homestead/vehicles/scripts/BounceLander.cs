using UnityEngine;
using System.Collections;
using System;

public class BounceLander : MonoBehaviour {
    public ParticleSystem[] Rockets = new ParticleSystem[4];

	// Use this for initialization
	void Start () {
	
	}

    private float lastAltimeter = 999f;
    private float secondsSinceLastAltimeterReading = 99f;
    private const float altimeterReadPeriod = .5f;
    private const float pulseDuration = 2f;
    private float pulseTime = 0f;
    private const float minDistanceToFireRockets = 175f;
    private bool haveRocketsFired = false;
    private bool rocketsFiring = false;
    private ConstantForce cf;

    // Update is called once per frame
    void Update () {
        if (haveRocketsFired)
        {
            if (rocketsFiring)
            {
                if (pulseTime > pulseDuration)
                {
                    ToggleRockets(false);
                }
                else
                {
                    pulseTime += Time.deltaTime;
                }
            }
            else
            {
                //bounce time!
            }
        }
        else
        {
            float altimeter = lastAltimeter;

            if (secondsSinceLastAltimeterReading > altimeterReadPeriod)
            {
                lastAltimeter = altimeter = GetRadarAltimetry();
                secondsSinceLastAltimeterReading = 0;
            }
            else
            {
                secondsSinceLastAltimeterReading += Time.deltaTime;
            }

            if (altimeter == -1)
            {
                //???
            }
            else if (altimeter <= minDistanceToFireRockets)
            {
                ToggleRockets(true);
            }
        }
	}

    private void ToggleRockets(bool state)
    {
        if (state)
        {
            print("firing rockets!");
            haveRocketsFired = true;
            this.cf = this.gameObject.AddComponent<ConstantForce>();
            this.cf.relativeForce = Vector3.up * 18f;
            rocketsFiring = true;
        }
        else
        {
            print("rockets done!");
            this.cf.enabled = false;
            rocketsFiring = false;
        }

        foreach(ParticleSystem sys in Rockets)
        {
            var emis = sys.emission;
            emis.enabled = rocketsFiring;

            if (emis.enabled)
                sys.Play();
            else
                sys.Stop();
        }
    }

    private float GetRadarAltimetry()
    {
        RaycastHit rayHit;

        if (Physics.Raycast(new Ray(transform.position, transform.TransformDirection(Vector3.down)), out rayHit))
        {
            print(rayHit.collider.name + " at " + rayHit.distance);
            return rayHit.distance;
        }

        return -1;
    }
}
