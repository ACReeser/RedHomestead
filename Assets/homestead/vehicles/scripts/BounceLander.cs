using UnityEngine;
using System.Collections;
using System;
using RedHomestead.Economy;
using RedHomestead.Simulation;
using System.Collections.Generic;

public class BounceLander : MonoBehaviour, IDeliveryScript
{
    public ParticleSystem[] Rockets = new ParticleSystem[4];
    public Transform airbagRoot, payloadCube, rocketRoot, payloadRoot;
    public Transform cratePrefab, vesselPrefab;

    // Use this for initialization
    void Start()
    {
        this.rigid = GetComponent<Rigidbody>();
        this.sphereCollider = GetComponent<SphereCollider>();
    }

    private const float InitialAltimeterReadPeriod = .5f;
    private const float EveryFrameAltimeterReadCutin = 300f;
    private float lastAltimeter = 999f;
    private float secondsSinceLastAltimeterReading = 99f;
    private const float PulseDuration = 2.5f;
    private float pulseTime = 0f;
    private const float MinDistanceToFireRockets = 165f;
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
                if (pulseTime > PulseDuration)
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

            if (altimeter < EveryFrameAltimeterReadCutin || secondsSinceLastAltimeterReading > InitialAltimeterReadPeriod)
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
            else if (altimeter <= MinDistanceToFireRockets)
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
        this.rigid.AddTorque(randomXZ, ForceMode.Impulse);
    }

    private const float deflateDuration = 2f;
    private float deflateTime = 0f;
    private SphereCollider sphereCollider;

    private IEnumerator DeflateAfterTime()
    {
        yield return new WaitForSeconds(10f);

        while (deflateTime < deflateDuration)
        {
            foreach (Transform t in airbagRoot)
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

    public void Deliver(Order o)
    {
        PackLander(o.LineItemUnits);
    }
    
    private void PackLander(ResourceCountDictionary lineItemUnits)
    {
        int i = 0;
        foreach(KeyValuePair<Matter, float> kvp in lineItemUnits)
        {
            for (int vol = 0; vol < kvp.Value; vol++)
            {
                Vector3 localPos = new Vector3(
                    (i % 4) > 1 ? .8f : -.8f,
                    i > 3 ? 1f : -1f,
                    (i % 2) == 0 ? .8f : -.8f
                    );

                CreateCratelike(kvp.Key, kvp.Value, localPos, payloadRoot);

                i++;
            }
        }
    }

    public static void CreateCratelike(Matter matter, float amount, Vector3 position, Transform parent = null)
    {
        Transform newT = Instantiate(EconomyManager.Instance.GetResourceCratePrefab(matter));

        var rc = newT.GetComponent<ResourceComponent>();
        rc.Data.ResourceType = matter;
        rc.Data.Quantity = amount;

        if (parent == null)
        {
            newT.position = position;
            newT.rotation = Quaternion.identity;
        }
        else
        {
            newT.SetParent(parent);
            newT.localPosition = position;
            newT.localRotation = Quaternion.identity;
        
            newT.GetComponent<Collider>().enabled = false;
            var rigibody = newT.GetComponent<Rigidbody>();
            rigibody.isKinematic = true;
            rigibody.useGravity = false;
        }
    }
}
