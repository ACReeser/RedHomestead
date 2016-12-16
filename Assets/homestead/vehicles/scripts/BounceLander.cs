using UnityEngine;
using System.Collections;
using System;

public class BounceLander : MonoBehaviour
{
    public ParticleSystem[] Rockets = new ParticleSystem[4];
    public Transform airbagRoot, payloadCube, rocketRoot, payloadRoot;

    // Use this for initialization
    void Start()
    {
        this.rigid = GetComponent<Rigidbody>();
        this.sphereCollider = GetComponent<SphereCollider>();
    }

    private float lastAltimeter = 999f;
    private float secondsSinceLastAltimeterReading = 99f;
    private const float altimeterReadPeriod = .5f;
    private const float pulseDuration = 2.5f;
    private float pulseTime = 0f;
    private const float minDistanceToFireRockets = 155f;
    private bool haveRocketsFired = false;
    private bool rocketsFiring = false;
    private ConstantForce cf;
    private Rigidbody rigid;

    public bool IsInteractable;

    // Update is called once per frame
    void Update()
    {
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
            this.cf.relativeForce = Vector3.up * 45f * rigid.mass;
            rocketsFiring = true;
        }
        else
        {
            print("rockets done!");
            this.cf.enabled = false;
            rocketsFiring = false;
        }

        foreach (ParticleSystem sys in Rockets)
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

    private bool initialCollision = true;
    void OnCollisionEnter(Collision collision)
    {
        Vector3 randomXZ = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));

        if (initialCollision)
        {
            initialCollision = false;
            //first bounce has a large random component
            randomXZ *= 200f;
            StartCoroutine(DeflateAfterTime());
        }
        else
        {
            randomXZ *= 50f;
        }
        this.rigid.AddForce(randomXZ, ForceMode.Impulse);
    }

    private const float deflateDuration = 2f;
    private float deflateTime = 0f;
    private SphereCollider sphereCollider;

    private IEnumerator DeflateAfterTime()
    {
        yield return new WaitForSeconds(10f);

        while (deflateTime < deflateDuration)
        {
            foreach(Transform t in airbagRoot)
            {
                t.localScale = Vector3.one * Mathf.Lerp(1f, .25f, deflateTime / deflateDuration);
            }
            deflateTime += Time.deltaTime;

            sphereCollider.radius = Mathf.Lerp(2.75f, 2f, deflateTime / deflateDuration);

            yield return new WaitForEndOfFrame();
        }

        this.payloadCube.tag = "airbagpayload";
        this.IsInteractable = true;
    }

    internal void Disassemble()
    {
        if (this.IsInteractable)
        {
            StartCoroutine(_Disassemble());
        }
    }

    private IEnumerator _Disassemble()
    {
        foreach (Transform t in airbagRoot)
        {
            t.gameObject.SetActive(false);
            yield return new WaitForSeconds(.1f);
        }
        foreach (Transform t in rocketRoot)
        {
            t.gameObject.SetActive(false);
            yield return new WaitForSeconds(.1f);
        }
        this.payloadCube.gameObject.SetActive(false);
        yield return new WaitForSeconds(.1f);
        this.rigid.isKinematic = true;
        this.rigid.useGravity = false;
        this.sphereCollider.enabled = false;
        yield return new WaitForSeconds(.1f);
        for (int i = payloadRoot.childCount - 1; i > -1; i--)
        {
            Transform t = payloadRoot.GetChild(i);
            t.GetComponent<Collider>().enabled = true;
            Rigidbody r = t.GetComponent<Rigidbody>();
            r.isKinematic = false;
            r.useGravity = true;
            t.transform.parent = null;
        }

        GameObject.Destroy(this.gameObject);
    }
}
