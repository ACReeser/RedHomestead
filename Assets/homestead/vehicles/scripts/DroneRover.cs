using RedHomestead.Economy;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[Serializable]
public class DroneRoverData: FacingData
{
    public ResourceUnitCountDictionary Delivery;
}

public class DroneRover : MonoBehaviour, IDataContainer<DroneRoverData> {
    public Transform BackBrace, Gantry, Shuttle, Canvas, WireGuide, GrabberPlate, BackGate, BedAnchor, SpawnStart, DropoffLocation;
    public Transform[] Latches = new Transform[4];
    public LineRenderer[] Wires = new LineRenderer[4];
    public Transform[] Wheels = new Transform[6];

    private const int NumberOfXSlots = 2;
    private const int NumberOfYSlots = 2;
    private const int NumberOfZSlots = 5;
    private IMovableSnappable[,,] Slots = new IMovableSnappable[NumberOfXSlots, NumberOfYSlots, NumberOfZSlots];
    
    private const float backBraceInZ = 1.89f;
    private Vector3 backBraceInPosition = new Vector3(0f, 0f, backBraceInZ);

    private const float CanvasFoldedScaleY = .1929f;
    private Vector3 canvasFoldedScale = new Vector3(1f, CanvasFoldedScaleY, 1f);

    private const float BackGateUpRotX = -90f;
    private Quaternion gateUpRotation = Quaternion.Euler(BackGateUpRotX, 180f, 0f);
    private const float BackGateDownRotX = 89.9f;
    private Quaternion gateDownRotation = Quaternion.Euler(BackGateDownRotX, 180f, 0f);

    private const float GantryY = 3.598f;
    private const float GantryBraceZ = -3.702f;
    private Vector3 GantryBracePosition = new Vector3(0f, GantryY, GantryBraceZ);
    private const float GantryDepth0Z = 1.287f;
    private Vector3 GantryDepth0Position = new Vector3(0f, GantryY, GantryDepth0Z);

    //#region grabber plate positions
    private const float GrabberTopZ = -1.95f;
    private Vector3 grabberTopPosition = new Vector3(0f, 0f, GrabberTopZ);

    private const float GrabberCargoTopZ = -2.25f;
    private Vector3 grabberCargoTopPosition = new Vector3(0f, 0f, GrabberCargoTopZ);

    private const float GrabberCargoBottomZ = -14.9f;
    private Vector3 grabberCargoBottomPosition = new Vector3(0f, 0f, GrabberCargoBottomZ);

    private const float GrabberGroundBottomZ = -29.72f;
    private Vector3 grabberGroundBottomPosition = new Vector3(0f, 0f, GrabberGroundBottomZ);

    private const float GrabberGroundTopZ = -17.89f;
    private Vector3 grabberGroundTopPosition = new Vector3(0f, 0f, GrabberGroundTopZ);
    //#endregion

    private const float ShuttleLeftX = 0.425f;
    private Vector3 ShuttleLeftPosition;
    private const float ShuttleRightX = -ShuttleLeftX;
    private Vector3 ShuttleRightPosition;

    private const float LatchClosedRot = 38.938f;
    private Quaternion Latch0ClosedRotation = Quaternion.Euler(LatchClosedRot, 0f, 0f);
    private Quaternion Latch1ClosedRotation = Quaternion.Euler(0f, -LatchClosedRot, -90f);
    private Quaternion Latch2ClosedRotation = Quaternion.Euler(LatchClosedRot, 0f, 0f);
    private Quaternion Latch3ClosedRotation = Quaternion.Euler(0f, -LatchClosedRot, -90f);
    private const float LatchOpenRotX = 0f;
    private const float BedCrateSeparation = 0.55f;
    private const float LatchDurationSeconds = 0.5f;
    private Quaternion Latch0OpenRotation = Quaternion.Euler(0f, 0f, 0f);
    private Quaternion Latch1OpenRotation = Quaternion.Euler(0f, 0f, -90f);
    private Quaternion Latch2OpenRotation = Quaternion.Euler(90f, 0f, 0f);
    private Quaternion Latch3OpenRotation = Quaternion.Euler(0f, -90f, -90f);

    private NavMeshAgent agent;

    // Use this for initialization
    void Start () {
        ShuttleLeftPosition = new Vector3(ShuttleLeftX, Shuttle.localPosition.y, this.Shuttle.localPosition.z);
        ShuttleRightPosition = new Vector3(ShuttleRightX, this.Shuttle.localPosition.y, this.Shuttle.localPosition.z);

        this.Canvas.localScale = Vector3.one;
        this.Gantry.localPosition = GantryDepth0Position;
        this.BackBrace.localPosition = backBraceInPosition;
        BackGate.localRotation = gateUpRotation;
        Shuttle.localPosition = ShuttleLeftPosition;
        GrabberPlate.localPosition = grabberTopPosition;

        foreach (LineRenderer r in Wires)
        {
            r.SetPosition(1, new Vector3(0f, 0f, 0f));
        }
        agent = this.GetComponent<NavMeshAgent>();
    }
    internal bool isDroppingOff = false;
    private Transform currentlyGrabbedCratelike;

    public DroneRoverData Data { get; set; }

    // Update is called once per frame
    void Update () {
		if (Input.GetKeyDown(KeyCode.Keypad0) && Input.GetKeyDown(KeyCode.Keypad0) && !isDroppingOff)
        {
            StartCoroutine(DropOff());
        }

        if (this.agent.velocity.sqrMagnitude > 0f)
        {
            foreach(Transform tire in Wheels)
            {
                tire.Rotate(Vector3.left, this.agent.velocity.sqrMagnitude * 1.2f, Space.Self);
            }
        }
	}

    private void Deliver(Order o)
    {
        this.Data = new DroneRoverData()
        {
            Delivery = o.LineItemUnits
        };

        int i = 0;
        foreach (Matter key in this.Data.Delivery.Keys)
        {
            var crateEnumerator = this.Data.Delivery.SquareMeters(key);
            while (crateEnumerator.MoveNext())
            {
                float volume = crateEnumerator.Current;

                int x, y, z;
                if (TryGetOpenSlot(out x, out y, out z))
                {
                    float xF = BedCrateSeparation;
                    if (x > 0)
                        xF = -xF; 
                    Vector3 cratelikePosition = this.BedAnchor.localPosition - new Vector3(xF, y * -1f, z * 1f);
                    var newCratelike = BounceLander.CreateCratelike(key, volume, cratelikePosition, this.transform);
                    Slots[x, y, z] = newCratelike.GetComponent<IMovableSnappable>();
                }
                i++;
            }
        }
    }

    private bool TryGetOpenSlot(out int iX, out int iY, out int iZ)
    {
        for (iZ = 0; iZ < NumberOfZSlots; iZ++)
        {
            for (iY = 0; iY < NumberOfYSlots; iY++)
            {
                for (iX = 0; iX < NumberOfXSlots; iX++)
                {
                    if (Slots[iX, iY, iZ] == null)
                    {
                        return true;
                    }
                }
            }
        }

        iX = iY = iZ = -1;
        return false;
    }

    private struct CargoBayPoint { public int X, Y, Z; }

    private IEnumerator<CargoBayPoint> UnpackSlots()
    {
        int iX;
        int iY;
        int iZ;
        for (iZ = NumberOfZSlots - 1; iZ > -1; iZ--)
        {
            for (iX = 0; iX < NumberOfXSlots; iX++)
            {
                for (iY = NumberOfYSlots - 1; iY > -1; iY--)
                {
                    if (Slots[iX, iY, iZ] != null)
                    {
                        yield return new CargoBayPoint()
                        {
                            X = iX,
                            Y = iY,
                            Z = iZ
                        };
                    }
                }
            }
        }
    }

    private IEnumerator DropOff()
    {
        this.Deliver(new Order()
        {
            LineItemUnits = new ResourceUnitCountDictionary()
            {
                { Matter.Hydrogen, 16 }
            }
        });
        this.transform.position = SpawnStart.position;
        this.agent.destination = DropoffLocation.position;
        yield return WaitUntilAtNextPosition();
        this.agent.enabled = false;

        isDroppingOff = true;
        yield return SetUp();

        var unpacker = UnpackSlots();
        int lastZ = -1;
        while (unpacker.MoveNext())
        {
            if (lastZ < 0)
                lastZ = unpacker.Current.Z;

            print(string.Format("Unpacking {0},{1},{2}", unpacker.Current.X, unpacker.Current.Y, unpacker.Current.Z));
            yield return SeekToCargoBay(unpacker.Current);

            if (unpacker.Current.Y == 0)
                yield return PickUpFromBottomCargo(unpacker);
            else
                yield return PickUpFromTopCargo(unpacker);

            if (lastZ != unpacker.Current.Z)
            {
                yield return ManualDrive(1.5f);
                lastZ = unpacker.Current.Z;
            }
            yield return SeekToBrace(unpacker.Current.X == 1);

            yield return SetDownOnGround(unpacker.Current.Y);
        }

        yield return SeekToCargoBay(new CargoBayPoint());
        yield return TearDown();
        this.agent.destination = SpawnStart.position;
        this.agent.enabled = true;
        yield return WaitUntilAtNextPosition();

        isDroppingOff = false;
        this.gameObject.SetActive(false);
    }

    private void GrabberGrabCratelike(IEnumerator<CargoBayPoint> unpacker)
    {
        currentlyGrabbedCratelike = Slots[unpacker.Current.X, unpacker.Current.Y, unpacker.Current.Z].transform;
        currentlyGrabbedCratelike.SetParent(GrabberPlate);
        currentlyGrabbedCratelike.localPosition = new Vector3(0f, 0f, -.5f);
    }

    private IEnumerator WaitUntilAtNextPosition()
    {        
        while (!AgentHasArrived())
        {
            yield return null;
        }
    }

    private bool AgentHasArrived()
    {
        return !this.agent.pathPending && (this.agent.remainingDistance == Mathf.Infinity || this.agent.remainingDistance <= this.agent.stoppingDistance) && (!this.agent.hasPath || this.agent.velocity.sqrMagnitude == 0f);
    }

    private IEnumerator SeekToCargoBay(CargoBayPoint point)
    {
        Vector3 gantryTo = GantryDepth0Position + (Vector3.back * point.Z);
        Vector3 shuttleTo = point.X == 1 ? ShuttleRightPosition : ShuttleLeftPosition;
        yield return SeekTo(Gantry.localPosition, gantryTo, Shuttle.localPosition, shuttleTo);
    }

    private IEnumerator SeekToBrace(bool isRight)
    {
        Vector3 shuttleTo = isRight ? ShuttleRightPosition : ShuttleLeftPosition;
        yield return SeekTo(Gantry.localPosition, GantryBracePosition, Shuttle.localPosition, shuttleTo);
    }

    private IEnumerator SeekTo(Vector3 gantryFrom, Vector3 gantryTo, Vector3 shuttleFrom, Vector3 shuttleTo)
    {
        float time = 0f;
        float duration = 1f;

        while (time < duration)
        {
            float t = time / duration;
            Gantry.localPosition = Vector3.Lerp(gantryFrom, gantryTo, t);
            Shuttle.localPosition = Vector3.Lerp(shuttleFrom, shuttleTo, t);
            yield return null;
            time += Time.deltaTime;
        }
    }

    private IEnumerator ManualDrive(float amount)
    {
        float forward = 0f;
        while (forward < amount)
        {
            this.transform.Translate(Vector3.forward * 0.025f, Space.Self);
            forward += 0.025f;
            foreach (Transform tire in Wheels)
            {
                tire.Rotate(Vector3.left, 5f, Space.Self);
            }
            yield return null;
        }
    }


    //grabber top, grabber bottom, unlatch, grabber top
    private IEnumerator SetDownOnGround(int y)
    {
        Vector3 targetBottomPositions = y == 1 ? grabberGroundBottomPosition : grabberGroundTopPosition;
        yield return MoveGrabber(grabberTopPosition, targetBottomPositions);
        
        yield return Latch(false);

        if (currentlyGrabbedCratelike != null)
        {
            BounceLander.EnlivenCratelike(currentlyGrabbedCratelike);
            currentlyGrabbedCratelike = null;
        }

        yield return MoveGrabber(targetBottomPositions, grabberTopPosition);
    }


    /// <summary>
    /// grabber top, grabber bottom, latch, grabber top
    /// </summary>
    /// <returns></returns>
    private IEnumerator PickUpFromGround()
    {
        yield return MoveGrabber(grabberTopPosition, grabberGroundBottomPosition);
        
        yield return Latch(true);

        yield return MoveGrabber(grabberGroundBottomPosition, grabberTopPosition);
    }

    /// <summary>
    /// grabs from top cargo position
    /// </summary>
    /// <param name="unpacker"></param>
    /// <returns></returns>
    private IEnumerator PickUpFromTopCargo(IEnumerator<CargoBayPoint> unpacker)
    {
        yield return MoveGrabber(grabberTopPosition, grabberCargoTopPosition);

        yield return Latch(true);

        GrabberGrabCratelike(unpacker);

        yield return MoveGrabber(grabberCargoTopPosition, grabberTopPosition);
    }

    /// <summary>
    /// grabs from top cargo position
    /// </summary>
    /// <param name="unpacker"></param>
    /// <returns></returns>
    private IEnumerator PickUpFromBottomCargo(IEnumerator<CargoBayPoint> unpacker)
    {
        yield return MoveGrabber(grabberTopPosition, grabberCargoBottomPosition);

        yield return Latch(true);

        GrabberGrabCratelike(unpacker);

        yield return MoveGrabber(grabberCargoBottomPosition, grabberTopPosition);
    }

    private IEnumerator MoveGrabber(Vector3 from, Vector3 to)
    {
        float time = 0f;
        float duration = 1f;

        while (time < duration)
        {
            float t = time / duration;
            GrabberPlate.localPosition = Vector3.Lerp(from, to, t);
            float distance = Shuttle.position.y - GrabberPlate.position.y;
            foreach(LineRenderer r in Wires)
            {
                r.SetPosition(1, new Vector3(0f, 0f, distance));
            }
            //update wires
            yield return null;
            time += Time.deltaTime;
        }

        GrabberPlate.localPosition = to;
    }

    private IEnumerator Latch(bool openToClose)
    {
        float time = 0f;
        float duration = LatchDurationSeconds;
        Quaternion from0;
        Quaternion from1;
        Quaternion from2;
        Quaternion from3;
        Quaternion to0;
        Quaternion to1;
        Quaternion to2;
        Quaternion to3;

        if (openToClose)
        {
            from0 = Latch0OpenRotation;
            from1 = Latch1OpenRotation;
            from2 = Latch2OpenRotation;
            from3 = Latch3OpenRotation;
            to0 = Latch0ClosedRotation;
            to1 = Latch1ClosedRotation;
            to2 = Latch2ClosedRotation;
            to3 = Latch3ClosedRotation;
        }
        else
        {
            from0 = Latch0ClosedRotation;
            from1 = Latch1ClosedRotation;
            from2 = Latch2ClosedRotation;
            from3 = Latch3ClosedRotation;
            to0 = Latch0OpenRotation;
            to1 = Latch1OpenRotation;
            to2 = Latch2OpenRotation;
            to3 = Latch3OpenRotation;
        }

        while (time < duration)
        {
            float t = time / duration;
            Latches[0].localRotation = Quaternion.Lerp(from0, to0, t);
            Latches[1].localRotation = Quaternion.Lerp(from1, to1, t);
            Latches[2].localRotation = Quaternion.Lerp(from2, to2, t);
            Latches[3].localRotation = Quaternion.Lerp(from3, to3, t);
            yield return null;
            time += Time.deltaTime;
        }
    }

    private IEnumerator SetUp()
    {
        return MoveBraceCanvasGate(backBraceInPosition, Vector3.zero, Vector3.one, canvasFoldedScale, gateUpRotation, gateDownRotation);
    }

    private IEnumerator TearDown()
    {
        return MoveBraceCanvasGate(Vector3.zero, backBraceInPosition, canvasFoldedScale, Vector3.one, gateDownRotation, gateUpRotation);
    }

    private IEnumerator MoveBraceCanvasGate(Vector3 braceFrom, Vector3 braceTo, Vector3 canvasFrom, Vector3 canvasTo, Quaternion gateFrom, Quaternion gateTo)
    {
        float time = 0f;
        float duration = 1f;

        while (time < duration)
        {
            float t = time / duration;
            BackBrace.localPosition = Vector3.Lerp(braceFrom, braceTo, t);
            Canvas.localScale = Vector3.Lerp(canvasFrom, canvasTo, t);
            BackGate.localRotation = Quaternion.Lerp(gateFrom, gateTo, t);
            yield return null;
            time += Time.deltaTime;
        }
    }
}
