using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneRover : MonoBehaviour {
    public Transform BackBrace, Gantry, Shuttle, Canvas, WireGuide, GrabberPlate, BackGate;
    public Transform[] Latches = new Transform[4];
    public LineRenderer[] Wires = new LineRenderer[4];

    
    private const float backBraceInZ = -1.89f;
    private Vector3 backBraceInPosition = new Vector3(0f, 0f, backBraceInZ);

    private const float CanvasFoldedScaleY = .1929f;
    private Vector3 canvasFoldedScale = new Vector3(1f, CanvasFoldedScaleY, 1f);

    private const float BackGateUpRotX = -90f;
    private Quaternion gateUpRotation = Quaternion.Euler(BackGateUpRotX, 0f, 0f);
    private const float BackGateDownRotX = 90f;
    private Quaternion gateDownRotation = Quaternion.Euler(BackGateDownRotX, 0f, 0f);

    private const float GantryY = 4.306025f;
    private const float GantryBraceZ = 3.702f;
    private Vector3 GantryBracePosition = new Vector3(0f, GantryY, GantryBraceZ);
    private const float GantryDepth0Z = -1.287f;
    private Vector3 GantryDepth0Position = new Vector3(0f, GantryY, GantryDepth0Z);

    private const float GrabberTopZ = -1.95f;
    private Vector3 grabberTopPosition = new Vector3(0f, 0f, GrabberTopZ);
    private const float GrabberBottomBottomZ = -39.72f;
    private Vector3 grabberBottomBottomPosition = new Vector3(0f, 0f, GrabberBottomBottomZ);
    private const float GrabberBottomTopZ = -25.89f;
    private Vector3 grabberBottomTopPosition = new Vector3(0f, 0f, GrabberBottomTopZ);

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
    private Quaternion Latch0OpenRotation = Quaternion.Euler(0f, 0f, 0f);
    private Quaternion Latch1OpenRotation = Quaternion.Euler(0f, 0f, -90f);
    private Quaternion Latch2OpenRotation = Quaternion.Euler(90f, 0f, 0f);
    private Quaternion Latch3OpenRotation = Quaternion.Euler(0f, -90f, -90f);

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
    }
    internal bool isDroppingOff = false;

	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown(KeyCode.Keypad0) && Input.GetKeyDown(KeyCode.Keypad0) && !isDroppingOff)
        {
            StartCoroutine(DropOff());
        }
	}

    private IEnumerator DropOff()
    {
        isDroppingOff = true;
        yield return SetUp();

        yield return SeekToCargoBay(0, false);
        yield return Latch(true);
        yield return SeekToBrace(false);
        yield return SetDownOnGround();
        yield return SeekToCargoBay(1, true);
        yield return Latch(true);
        yield return SeekToBrace(true);
        yield return SetDownOnGround();

        yield return SeekToCargoBay(0, false);
        yield return TearDown();
        isDroppingOff = false;
    }

    private IEnumerator SeekToCargoBay(int depth, bool isRight)
    {
        Vector3 gantryTo = GantryDepth0Position + (Vector3.forward * depth);
        Vector3 shuttleTo = isRight ? ShuttleRightPosition : ShuttleLeftPosition;
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

    //grabber top, grabber bottom, unlatch, grabber top
    private IEnumerator SetDownOnGround()
    {
        yield return MoveGrabber(grabberTopPosition, grabberBottomBottomPosition);
        
        yield return Latch(false);

        yield return MoveGrabber(grabberBottomBottomPosition, grabberTopPosition);
    }

    //grabber top, grabber bottom, latch, grabber top
    private IEnumerator PickUpFromGround()
    {
        yield return MoveGrabber(grabberTopPosition, grabberBottomBottomPosition);
        
        yield return Latch(true);

        yield return MoveGrabber(grabberBottomBottomPosition, grabberTopPosition);
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
    }

    private IEnumerator Latch(bool openToClose)
    {
        float time = 0f;
        float duration = 1f;
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
