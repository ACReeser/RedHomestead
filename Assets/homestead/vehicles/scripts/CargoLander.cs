using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Economy;

public class CargoLander : MonoBehaviour, ICrateSnapper, ITriggerSubscriber {
    private const int MinimumAltitude = 300;
    private const float AltitudeRangeAboveMinimum = 100f;
    private const float LandingAltitudeAboveLandingZone = 2.25f;
    private const float LandingTimeSeconds = 20f;
    private const float LandingStageTimeSeconds = LandingTimeSeconds / 2f;
    private const float DoorMoveDuration = 2f;

    public enum FlightState { Landing = -1, Disabled, TakingOff = 1, Landed = 9 }

    public static CargoLander Instance;
    
    public Transform LanderPivot, LanderHinge, Lander;
    public ParticleSystem RocketFire, RocketSmoke;
    public Transform[] Ramps;
    public AudioClip rocket, electric;
    public AudioSource rocketSource, doorSource;

    internal FlightState State { get; private set; }

    internal LandingZone LZ { get; private set; }
    private DoorRotationLerpContext[] ramps;
    private Dictionary<TriggerForwarder, ResourceComponent> Bays;

    internal void Deliver(Order o, LandingZone z)
    {
        this.LZ = z;
        InitBays();

        int i = 0;
        TriggerForwarder[] slots = new TriggerForwarder[Bays.Keys.Count];
        Bays.Keys.CopyTo(slots, 0);

        foreach (KeyValuePair<Matter, float> kvp in o.LineItemUnits)
        {
            var crateEnumerator = o.LineItemUnits.SquareMeters(kvp.Key);
            while (crateEnumerator.MoveNext())
            {
                float volume = crateEnumerator.Current;

                TriggerForwarder child = slots[i];
                
                var t = BounceLander.CreateCratelike(kvp.Key, kvp.Value, Vector3.zero, this.Lander);

                Bays[child] = t.GetComponent<ResourceComponent>();

                i++;
            }
        }

        Land();
    }

    private void InitBays()
    {
        if (Bays == null)
        {
             Bays = new Dictionary<TriggerForwarder, ResourceComponent>();
            foreach (TriggerForwarder t in GetComponentsInChildren<TriggerForwarder>())
            {
                Bays.Add(t, null);
            }
        }
    }

    // Use this for initialization
    void Start () {
        Instance = this;

        ramps = new DoorRotationLerpContext[Ramps.Length];
        int i = 0;
        foreach(Transform t in Ramps)
        {
            ramps[i] = new DoorRotationLerpContext(t, t.localRotation, t.localRotation * Quaternion.Euler(0f, -150f, 0f), DoorMoveDuration);
            i++;
        }

        InitBays();

        rocketSource.clip = rocket;
        doorSource.clip = electric;

        if (State == FlightState.Disabled)
            Lander.gameObject.SetActive(false);
	}

    private Vector3 airborneLanderPosition, landedLanderPosition;
    private Vector3 airborneLanderHinge = Vector3.zero;
    private Vector3 landedLanderHinge = new Vector3(0, 0, 90f);
    private Coroutine movement;
    public void Land()
    {
        if (movement != null)
            StopCoroutine(movement);

        SunOrbit.Instance.ResetToNormalTime();
        GuiBridge.Instance.ShowNews(NewsSource.IncomingLander);
        LanderHinge.localRotation = Quaternion.identity;
        CalculateAndPositionFlightArc();
        Lander.localPosition = airborneLanderPosition;

        State = FlightState.Landing;
        movement = StartCoroutine(DoLand());
    }

    private void CalculateAndPositionFlightArc()
    {
        this.transform.position = LZ.transform.position+Vector3.up*2.25f;
        float minAltitude = LZ.transform.position.y + MinimumAltitude;
        float startAltitude = UnityEngine.Random.Range(minAltitude, minAltitude + AltitudeRangeAboveMinimum);
        float randomAngle = UnityEngine.Random.Range(0, 360);
        LanderHinge.localPosition = new Vector3(startAltitude, 0, 0);
        LanderPivot.localRotation = Quaternion.Euler(0f, randomAngle, 0f);
        airborneLanderPosition = new Vector3((float)(startAltitude * Math.Sqrt(2f)), startAltitude, 0);
        landedLanderPosition = new Vector3(0, startAltitude, 0f);
    }

    public void TakeOff()
    {
        if (movement != null)
            StopCoroutine(movement);

        //GuiBridge.Instance.ShowNews(NewsSource.IncomingCargo);
        CalculateAndPositionFlightArc();
        Lander.localPosition = landedLanderPosition;

        State = FlightState.TakingOff;
        movement = StartCoroutine(DoTakeOff());
    }


    private IEnumerator DoFly(Vector3 startPos, Vector3 endPos)
    {
        float time = 0f;

        while (time < LandingStageTimeSeconds)
        {
            Lander.localPosition = Vector3.Lerp(startPos, endPos, time / LandingStageTimeSeconds);
            yield return null;
            time += Time.deltaTime;
        }
        Lander.localPosition = endPos;
    }

    private IEnumerator DoFireRockets(Vector3 startHinge, Vector3 endHinge, bool easeOut = true)
    {
        float time = 0f;

        RocketFire.Play();
        RocketSmoke.Play();
        rocketSource.Play();
        
        if (easeOut)
        {
            //ease-out
            //landing
            //copied and pasted by below
            while (time < LandingStageTimeSeconds)
            {
                rocketSource.volume = Mathf.Min(1f, LandingStageTimeSeconds - time);

                LanderHinge.localRotation = Quaternion.Euler(
                    Mathfx.Sinerp(startHinge, endHinge, time / LandingStageTimeSeconds)
                    );
                yield return null;
                time += Time.deltaTime;
            }
        }
        else
        {
            //ease-in
            //take-off
            //copied and pasted from above, just using coserp
            while (time < LandingStageTimeSeconds)
            {
                rocketSource.volume = Mathf.Max(0f, time);
                print(rocketSource.volume);

                LanderHinge.localRotation = Quaternion.Euler(
                    Mathfx.Coserp(startHinge, endHinge, time / LandingStageTimeSeconds)
                    );
                yield return null;
                time += Time.deltaTime;
            }
        }
        LanderHinge.localRotation = Quaternion.Euler(endHinge);
        RocketFire.Stop();
        RocketSmoke.Stop();
        rocketSource.Stop();
    }

    private IEnumerator DoTakeOff()
    {
        yield return new WaitForSeconds(1f);

        doorSource.Play();
        ToggleAllRamps();
        yield return new WaitForSeconds(DoorMoveDuration);
        doorSource.Stop();
        foreach (var kvp in Bays)
        {
            if (kvp.Value != null)
            {
                GameObject.Destroy(kvp.Value.transform.root.gameObject);
            }
        }

        yield return new WaitForSeconds(3f);

        yield return DoFireRockets(landedLanderHinge, airborneLanderHinge, false);

        yield return DoFly(landedLanderPosition, airborneLanderPosition);

        movement = null;

        State = FlightState.Disabled;
        Lander.gameObject.SetActive(false);
    }

    private IEnumerator DoLand()
    {
        Lander.gameObject.SetActive(true);

        yield return DoFly(airborneLanderPosition, landedLanderPosition);

        yield return DoFireRockets(airborneLanderHinge, landedLanderHinge);

        foreach(var kvp in Bays)
        {
            if (kvp.Value != null)
            {
                kvp.Value.transform.SetParent(null);
                kvp.Value.SnapCrate(this, kvp.Key.transform.position, globalRotation: kvp.Key.transform.rotation);
                kvp.Value.GetComponent<Collider>().enabled = true;
            }
        }

        yield return new WaitForSeconds(3f);

        doorSource.Play();
        ToggleAllRamps();

        yield return new WaitForSeconds(DoorMoveDuration);
        doorSource.Stop();

        movement = null;

        State = FlightState.Landed;
    }

    private void ToggleAllRamps()
    {
        ramps[0].Toggle(StartCoroutine);
        ramps[1].Toggle(StartCoroutine);
        ramps[2].Toggle(StartCoroutine);
        ramps[3].Toggle(StartCoroutine);
        ramps[4].Toggle(StartCoroutine);
        ramps[5].Toggle(StartCoroutine);
        ramps[6].Toggle(StartCoroutine);
        ramps[7].Toggle(StartCoroutine);
    }

    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable mov)
    {
        ResourceComponent res = mov as ResourceComponent;
        if (Bays.ContainsKey(child) && Bays[child] == null && res != null)
        {
            res.SnapCrate(this, child.transform.position, globalRotation: child.transform.rotation);
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        foreach(var kvp in Bays)
        {
            if (kvp.Value == (object)detaching)
            {
                Bays[kvp.Key] = null;
            }
        }
    }
}
