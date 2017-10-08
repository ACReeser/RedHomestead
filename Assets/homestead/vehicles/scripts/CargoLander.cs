using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RedHomestead.Economy;
using RedHomestead.Persistence;
using System.Linq;

[Serializable]
public class CargoLanderData: FacingData
{
    public string LZInstanceID;
    public ResourceUnitCountDictionary Delivery;
    public CargoLander.FlightState State;
    public int BallisticAngle = -1;
    [NonSerialized]
    public bool FromSave = true;
}

public class CargoLander : MonoBehaviour, ICrateSnapper, ITriggerSubscriber, IDataContainer<CargoLanderData> {
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

    internal LandingZone LZ { get; private set; }

    public CargoLanderData Data { get; set; }

    private DoorRotationLerpContext[] ramps;
    private Dictionary<TriggerForwarder, ResourceComponent> Bays;
    private bool rampsDown = false;

    internal void Deliver(Order o, LandingZone z)
    {
        this.LZ = z;
        this.transform.position = LZ.transform.position + Vector3.up * 2.25f;
        InitBays();

        this.Data = new CargoLanderData()
        {
            LZInstanceID = this.LZ.Data.LZInstanceID,
            Transform = this.transform,
            Delivery = o.LineItemUnits,
            FromSave = false //this gets called before Start, so this is a way to signal Start not to re-call Land()
        };
        
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

        if (Data == null)
        {
            Lander.gameObject.SetActive(false);
        }
        //only do stuff if we're starting from a loaded game
        else if (Data.FromSave)
        {
            LZ = FindObjectsOfType<LandingZone>().Where(x => x.Data.LZInstanceID == this.Data.LZInstanceID).FirstOrDefault();

            if (Data.State == FlightState.Disabled)
                Lander.gameObject.SetActive(false);
            else
            {
                this.Lander.gameObject.SetActive(true);
                this.transform.SetPositionAndRotation(this.Data.Position, this.Data.Rotation);
            }

            if (Data.State == FlightState.Landing)
                Land();
            else if (Data.State == FlightState.Landed)
            {
                this.Lander.SetPositionAndRotation(this.Data.Position, this.Data.Rotation);
                if (!rampsDown)
                    ToggleAllRamps();
            }
            else if (Data.State == FlightState.TakingOff)
            {
                this.Lander.SetPositionAndRotation(this.Data.Position, this.Data.Rotation);
                TakeOff();
            }
        }
	}

    private Vector3 airborneLanderPosition, landedLanderLocalPosition;
    private Vector3 airborneLanderHinge = Vector3.zero;
    private Vector3 landedLanderHinge = new Vector3(0, 0, 90f);
    private Coroutine movement;
    private Coroutine countdownCoroutine;
    private Coroutine unsnapTimer;

    public void Land()
    {
        if (movement != null)
            StopCoroutine(movement);

        SunOrbit.Instance.ResetToNormalTime();
        GuiBridge.Instance.ShowNews(NewsSource.IncomingLander);

        SetState(FlightState.Landing);

        movement = StartCoroutine(DoLand());
    }

    private void CalculateAndPositionFlightArc()
    {
        if (Data.BallisticAngle < 0)
            Data.BallisticAngle = UnityEngine.Random.Range(0, 360);

        float minAltitude = LZ.transform.position.y + MinimumAltitude;
        float startAltitude = UnityEngine.Random.Range(minAltitude, minAltitude + AltitudeRangeAboveMinimum);
        LanderHinge.localPosition = new Vector3(startAltitude, 0, 0);
        LanderPivot.localRotation = Quaternion.Euler(0f, Data.BallisticAngle, 0f);
        airborneLanderPosition = new Vector3((float)(startAltitude * Math.Sqrt(2f)), startAltitude, 0);
        landedLanderLocalPosition = new Vector3(0, startAltitude, 0f);
    }

    public void TakeOff()
    {
        if (movement != null)
            StopCoroutine(movement);
        
        CalculateAndPositionFlightArc();

        SetState(FlightState.TakingOff);

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
        if (rampsDown)
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

        for (int i = 0; i < 10; i++)
        {
            GuiBridge.Instance.ShowNews(NewsSource.LanderCountdown.CloneWithSuffix(": " + (10 - i) + "s"));
            yield return new WaitForSeconds(1f);
        }
        var liftoffNews = NewsSource.LanderCountdown.CloneWithSuffix(": Liftoff");
        liftoffNews.DurationMilliseconds = 3000f;
        GuiBridge.Instance.ShowNews(liftoffNews);

        yield return DoFireRockets(landedLanderHinge, airborneLanderHinge, false);

        yield return DoFly(landedLanderLocalPosition, airborneLanderPosition);

        movement = null;

        Data.State = FlightState.Disabled;
        Lander.gameObject.SetActive(false);
    }

    private IEnumerator DoLand()
    {
        Lander.gameObject.SetActive(true);

        yield return DoFly(airborneLanderPosition, landedLanderLocalPosition);

        yield return DoFireRockets(airborneLanderHinge, landedLanderHinge);

        int i = 0;
        TriggerForwarder[] slots = new TriggerForwarder[Bays.Keys.Count];
        Bays.Keys.CopyTo(slots, 0);
        foreach (Matter key in this.Data.Delivery.Keys)
        {
            var crateEnumerator = this.Data.Delivery.SquareMeters(key);
            while (crateEnumerator.MoveNext())
            {
                float volume = crateEnumerator.Current;

                TriggerForwarder child = slots[i];

                var t = BounceLander.CreateCratelike(key, volume, Vector3.zero, this.Lander);

                Bays[child] = t.GetComponent<ResourceComponent>();

                i++;
            }
        }
        this.Data.Delivery = null;

        foreach (var kvp in Bays)
        {
            if (kvp.Value != null)
            {
                kvp.Value.transform.SetParent(null);
                kvp.Value.SnapCrate(this, kvp.Key.transform.position, globalRotation: kvp.Key.transform.rotation);
                kvp.Value.GetComponent<Collider>().enabled = true;
            }
        }

        SetState(FlightState.Landed);

        yield return new WaitForSeconds(3f);

        doorSource.Play();
        if (!rampsDown)
            ToggleAllRamps();

        yield return new WaitForSeconds(DoorMoveDuration);
        doorSource.Stop();

        movement = null;

    }

    private void SetState(FlightState state)
    {
        Data.State = state;
        switch (state)
        {
            case FlightState.Landing:
                LanderHinge.localRotation = Quaternion.identity;
                CalculateAndPositionFlightArc();
                Lander.localPosition = airborneLanderPosition;
                Data.State = FlightState.Landing;
                break;
            case FlightState.Landed:
                Lander.localPosition = landedLanderLocalPosition;
                break;
            case FlightState.TakingOff:
                Lander.localPosition = landedLanderLocalPosition;
                break;
        }
    }

    private void ToggleAllRamps()
    {
        rampsDown = !rampsDown;
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
        if (unsnapTimer == null && Bays.ContainsKey(child) && Bays[child] == null && res != null)
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

        if (Data.State == FlightState.Landed)
        {
            if (Bays.Values.All(x => x == null))
            {
                countdownCoroutine = StartCoroutine(BeginCountdown());
            }
        }

        unsnapTimer = StartCoroutine(detachTimer());
    }

    private IEnumerator detachTimer()
    {
        yield return new WaitForSeconds(1f);
        unsnapTimer = null;
    }

    private IEnumerator BeginCountdown()
    {
        var oneHour = NewsSource.LanderCountdown.CloneWithSuffix(": 1hr");
        oneHour.DurationMilliseconds = 3000f;
        GuiBridge.Instance.ShowNews(oneHour);
        yield return new WaitForSeconds(60f);
        TakeOff();
    }
}
