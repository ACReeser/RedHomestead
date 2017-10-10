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
    public int BallisticAngle;
    public float Altitude;

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

    /// <summary>
    /// Set the LineItems from the order O as arriving now at landing zone Z
    /// </summary>
    /// <param name="o"></param>
    /// <param name="z"></param>
    internal void Deliver(Order o, LandingZone z)
    {
        this.LZ = z;
        this.transform.position = LZ.transform.position + Vector3.up * 2.25f;
        InitBays();

        float minAltitude = LZ.transform.position.y + MinimumAltitude;
        this.Data = new CargoLanderData()
        {
            LZInstanceID = this.LZ.Data.LZInstanceID,
            Transform = this.transform,
            Delivery = o.LineItemUnits,
            BallisticAngle = UnityEngine.Random.Range(0, 360),
            Altitude = UnityEngine.Random.Range(minAltitude, minAltitude + AltitudeRangeAboveMinimum),
            FromSave = false //this gets called before Start, so this is a way to signal Start not to re-call Land()
        };
        InitHingePositions();

        Land();
    }

    /// <summary>
    /// Using Data, find pivot points from angle and altitude
    /// </summary>
    private void InitHingePositions()
    {
        LanderHinge.localPosition = new Vector3(Data.Altitude, 0, 0);
        LanderPivot.localRotation = Quaternion.Euler(0f, Data.BallisticAngle, 0f);
        airborneLanderPosition = new Vector3((float)(Data.Altitude * Math.Sqrt(2f)), Data.Altitude, 0);
        landedLanderLocalPosition = new Vector3(0, Data.Altitude, 0f);
    }

    /// <summary>
    /// Finds trigger forwarder children and adds them to the Bays dict
    /// </summary>
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
    void Start()
    {
        Instance = this;

        InitializeRamps();
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

            this.transform.SetPositionAndRotation(this.Data.Position, this.Data.Rotation);
            InitHingePositions();
            SetState(Data.State);
            
            switch (Data.State)
            {
                case FlightState.Landing:
                    Land();
                    break;
                case FlightState.Landed:
                    if (!rampsDown)
                        ToggleAllRamps();
                    break;
                case FlightState.TakingOff:
                    TakeOff();
                    break;
            }
        }
    }

    /// <summary>
    /// Using the public Ramps array, create door rotate lerp contexts for them
    /// </summary>
    private void InitializeRamps()
    {
        ramps = new DoorRotationLerpContext[Ramps.Length];
        int i = 0;
        foreach (Transform t in Ramps)
        {
            ramps[i] = new DoorRotationLerpContext(t, t.localRotation, t.localRotation * Quaternion.Euler(0f, -150f, 0f), DoorMoveDuration);
            i++;
        }
    }

    private Vector3 airborneLanderPosition, landedLanderLocalPosition;
    private Vector3 airborneLanderHinge = Vector3.zero;
    private Vector3 landedLanderHinge = new Vector3(0, 0, 90f);
    private Coroutine movement;
    private Coroutine countdownCoroutine;
    private Coroutine unsnapTimer;

    /// <summary>
    /// Public entry point for Landing sequence - starts coroutine
    /// </summary>
    public void Land()
    {
        if (movement != null)
            StopCoroutine(movement);

        SunOrbit.Instance.ResetToNormalTime();
        GuiBridge.Instance.ShowNews(NewsSource.IncomingLander);

        SetState(FlightState.Landing);

        movement = StartCoroutine(DoLand());
    }

    /// <summary>
    /// Public entry point for Take Off sequence - starts coroutine
    /// </summary>
    public void TakeOff()
    {
        if (movement != null)
            StopCoroutine(movement);

        SunOrbit.Instance.ResetToNormalTime();
        SetState(FlightState.TakingOff);

        movement = StartCoroutine(DoTakeOff());
        countdownCoroutine = null;
    }

    /// <summary>
    /// Flies from startPos to endPos (up in the air)
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
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

    /// <summary>
    /// fires rockets while rotating from startHinge to endHinge
    /// </summary>
    /// <param name="startHinge"></param>
    /// <param name="endHinge"></param>
    /// <param name="easeOut"></param>
    /// <returns></returns>
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
        RocketFire.Stop();
        RocketSmoke.Stop();
        rocketSource.Stop();
    }

    /// <summary>
    /// take off sequence
    /// </summary>
    /// <returns></returns>
    private IEnumerator DoTakeOff()
    {
        yield return new WaitForSeconds(1f);

        #region pre-takeoff ramps up
        doorSource.Play();
        if (rampsDown)
            ToggleAllRamps();
        yield return new WaitForSeconds(DoorMoveDuration);
        doorSource.Stop();
        #endregion

        //destroy any crates attached, POOF!
        var resources = Bays.Values.ToArray();
        for (int i = resources.Length - 1; i > -1; i--)
        {
            if (resources[i] != null)
            {
                GameObject.Destroy(resources[i].transform.root.gameObject);
            }
        }

        #region takeoff countdown
        for (int i = 0; i < 10; i++)
        {
            GuiBridge.Instance.ShowNews(NewsSource.LanderCountdown.CloneWithSuffix(": " + (10 - i) + "s"));
            yield return new WaitForSeconds(1f);
        }
        var liftoffNews = NewsSource.LanderCountdown.CloneWithSuffix(": Liftoff");
        liftoffNews.DurationMilliseconds = 3000f;
        GuiBridge.Instance.ShowNews(liftoffNews);
        #endregion

        yield return DoFireRockets(landedLanderHinge, airborneLanderHinge, false);

        yield return DoFly(landedLanderLocalPosition, airborneLanderPosition);

        movement = null;

        SetState(FlightState.Disabled);
    }

    /// <summary>
    /// landing sequence
    /// </summary>
    /// <returns></returns>
    private IEnumerator DoLand()
    {
        Lander.gameObject.SetActive(true);

        yield return DoFly(airborneLanderPosition, landedLanderLocalPosition);

        bool turnedLightsOn = false;
        if (Game.Current.Environment.CurrentHour > 20 || Game.Current.Environment.CurrentHour < 6)
        {
            this.LZ.ToggleLights(true);
            turnedLightsOn = true;
        }

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

        if (turnedLightsOn)
            this.LZ.ToggleLights(false);

        movement = null;
    }

    /// <summary>
    /// handles common state like hinge angles, lander position, lander enabled
    /// </summary>
    /// <param name="state"></param>
    private void SetState(FlightState state)
    {
        Data.State = state;

        Lander.gameObject.SetActive(state != FlightState.Disabled);

        Quaternion effectiveLanderHingeLocalRotation = Quaternion.identity;
        Vector3 effectiveLanderLocalPosition = Vector3.zero;

        switch (state)
        {
            case FlightState.Landing:
                effectiveLanderHingeLocalRotation = Quaternion.Euler(airborneLanderHinge);
                effectiveLanderLocalPosition      = airborneLanderPosition;
                break;
            case FlightState.Landed:
                effectiveLanderHingeLocalRotation = Quaternion.Euler(landedLanderHinge);
                effectiveLanderLocalPosition      = landedLanderLocalPosition;
                break;
            case FlightState.TakingOff:
                effectiveLanderHingeLocalRotation = Quaternion.Euler(landedLanderHinge);
                effectiveLanderLocalPosition      = landedLanderLocalPosition;
                break;
        }

        LanderHinge.localRotation = effectiveLanderHingeLocalRotation;
        Lander.localPosition = effectiveLanderLocalPosition;
    }

    /// <summary>
    /// toggles all ramps
    /// </summary>
    private void ToggleAllRamps()
    {
        rampsDown = !rampsDown;

        //unrolled loop for performance
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
                break;
            }
        }

        if (Data.State == FlightState.Landed && countdownCoroutine == null && Bays.Values.All(x => x == null))
        {
            countdownCoroutine = StartCoroutine(BeginCountdown());
        }

        unsnapTimer = StartCoroutine(detachTimer());
    }

    private IEnumerator detachTimer()
    {
        yield return new WaitForSeconds(1f);
        unsnapTimer = null;
    }

    /// <summary>
    /// precursor countdown to takeoff
    /// </summary>
    /// <returns></returns>
    private IEnumerator BeginCountdown()
    {
        var oneHour = NewsSource.LanderCountdown.CloneWithSuffix(": 1hr");
        oneHour.DurationMilliseconds = 3000f;
        GuiBridge.Instance.ShowNews(oneHour);
        yield return new WaitForSeconds(SunOrbit.GameSecondsPerMartianMinute*60f);
        TakeOff();
    }
}
