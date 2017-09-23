using RedHomestead.Buildings;
using RedHomestead.Crafting;
using RedHomestead.Electricity;
using RedHomestead.Persistence;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ThreeDPrinterFlexData
{
    public float Progress;
    public Matter Printing;
    public float Duration;
}

public class ThreeDPrinter : Converter, IDoorManager, ITriggerSubscriber, ICrateSnapper, IPowerConsumerToggleable, IFlexDataContainer<MultipleResourceModuleData, ThreeDPrinterFlexData>
{
    public Transform printArm, printHead, printArmHarness, laserAnchor, laserMidpoint;
    public LineRenderer laser;

    private DoorRotationLerpContext lerp;
    private const float maxArmZ = .96f;
    private const float minArmZ = -.96f;
    private const float maxArmY = 2.337844f;
    private const float minArmY = .34f;
    private const float maxHeadX = .824f;
    private const float minHeadX = -.824f;
    private Coroutine currentPrint;
    private Vector3 resetPosition;
    private static Vector3 crateStartLocalPosition = new Vector3(0f, 0.578f, 0f);

    public ThreeDPrinterFlexData FlexData { get; set; }

    public void ToggleDoor(Transform door)
    {
        //assumes all door transforms start shut
        if (lerp == null)
            lerp = new DoorRotationLerpContext(door, door.localRotation, Quaternion.Euler(-60, 0, -90f), .8f);
        
        lerp.Toggle(StartCoroutine);
    }

    protected override void OnStart()
    {
        base.OnStart();
        this.RefreshPowerSwitch();
        resetPosition = printArm.localPosition;

        if (FlexData != null && FlexData.Printing != Matter.Unspecified)
            StartPrint();
    }

    public Material cutawayMaterial;
    private MeshRenderer currentPrintRenderer;
    public Transform InProgressPrintCrate;
    internal void BeginPrinting(Matter component)
    {
        FlexData.Printing = component;
        FlexData.Progress = 0f;
        FlexData.Duration = Crafting.PrinterData[component].BuildTime * SunOrbit.GameSecondsPerMartianMinute * 60f;

        StartPrint();
    }

    private void StartPrint()
    {
        if (currentPrint != null)
            StopCoroutine(currentPrint);

        currentPrintRenderer = InProgressPrintCrate.GetChild(0).GetComponent<MeshRenderer>();
        currentPrintRenderer.enabled = true;
        cutawayMaterial.mainTexture = currentPrintRenderer.material.mainTexture;
        currentPrintRenderer.material = cutawayMaterial;

        currentPrint = StartCoroutine(ArmPrint());
    }

    private IEnumerator ArmPrint()
    {
        MainMenu.LerpContext armY = new MainMenu.LerpContext()
        {
            Space = Space.Self
        };
        MainMenu.LerpContext headX = GetNextHead();
        float alreadyPassed = FlexData.Progress * FlexData.Duration;
        armY.Seed(Vector3.up * minArmY, Vector3.up * maxArmY, FlexData.Duration - alreadyPassed, _time: alreadyPassed);
        bool back = false;
        laser.enabled = true;

        while (!armY.Done)
        {
            FlexData.Progress = armY.T;

            armY.Tick(printArm);
            headX.Tick(printHead);
            printArm.localPosition = new Vector3(printArm.localPosition.x, printArm.localPosition.y, back ? Mathf.Lerp(minArmZ, maxArmZ, headX.T) : Mathf.Lerp(maxArmZ, minArmZ, headX.T));
            printArmHarness.localPosition = new Vector3(0f, 0f, printArm.localPosition.z);
            UpdateLaser();
            currentPrintRenderer.material.SetFloat("_showPercentY", armY.T * 100f);

            yield return null;

            if (headX.Done)
            {
                headX = GetNextHead();
                back = !back;
            }
        }

        laser.enabled = false;
        currentPrintRenderer.enabled = false;

        FinishPrinting();

        MainMenu.LerpContext reset = new MainMenu.LerpContext()
        {
            Space = Space.Self
        };
        reset.Seed(printArm.localPosition, resetPosition, 2f);
        while (!reset.Done)
        {
            reset.Tick(printArm);
            printArmHarness.localPosition = new Vector3(0f, 0f, printArm.localPosition.z);
            yield return null;
        }
    }

    private void FinishPrinting()
    {
        BounceLander.CreateCratelike(FlexData.Printing, 1f, this.transform.TransformPoint(crateStartLocalPosition));
        FlexData.Printing = Matter.Unspecified;
        FlexData.Progress = 0f;

        if (GuiBridge.Instance.Printer.Showing)
            GuiBridge.Instance.Printer.SetShowing(true, this);

        GuiBridge.Instance.ShowNews(NewsSource.PrintingComplete);
        SunOrbit.Instance.ResetToNormalTime();
    }

    private void UpdateLaser()
    {
        laser.SetPosition(0, laserAnchor.position);
        laser.SetPosition(1, new Vector3(laserAnchor.position.x, laserAnchor.position.y, laserMidpoint.position.z));
        laser.SetPosition(2, laserMidpoint.position);
        laser.SetPosition(3, new Vector3(printHead.position.x, laserMidpoint.position.y, laserMidpoint.position.z));
        laser.SetPosition(4, new Vector3(printHead.position.x, transform.position.y, printHead.position.z));
    }

    internal bool Has(ResourceEntry req)
    {
        return (LeftInput != null && LeftInput.Data.Container.MatterType == req.Type && LeftInput.Data.Container.CurrentAmount >= req.Count) ||
               (RightInput != null && RightInput.Data.Container.MatterType == req.Type && RightInput.Data.Container.CurrentAmount >= req.Count);
    }

    private MainMenu.LerpContext GetNextHead()
    {
        var lerp = new MainMenu.LerpContext()
        {
            Space = Space.Self
        };
        lerp.Seed(printHead.localPosition, new Vector3(UnityEngine.Random.Range(minHeadX, maxHeadX), 0f, 0f), 1f);
        return lerp;
    }

    public TriggerForwarder leftInputTrigger, rightInputTrigger;
    public ResourceComponent LeftInput { get; private set; }
    public ResourceComponent RightInput { get; private set; }

    public override float WattsConsumed
    {
        get
        {
            return ElectricityConstants.WattsPerBlock * 5f;
        }
    }

    public MeshFilter powerCabinet;
    public MeshFilter PowerCabinet
    {
        get
        {
            return powerCabinet;
        }
    }
    
    public void OnChildTriggerEnter(TriggerForwarder child, Collider c, IMovableSnappable res)
    {
        ResourceComponent resComp = res.transform.GetComponent<ResourceComponent>();
        if (resComp != null)
        {
            if (resComp.Data.Container.MatterType.Is3DPrinterFeedstock())
            {
                if (child == leftInputTrigger && LeftInput == null)
                {
                    LeftInput = resComp;
                    res.SnapCrate(this, child.transform.position, globalRotation: child.transform.rotation);
                }
                else if (child == rightInputTrigger && RightInput == null)
                {
                    RightInput = resComp;
                    res.SnapCrate(this, child.transform.position, globalRotation: child.transform.rotation);
                }
            }
            else
            {
                GuiBridge.Instance.ShowNews(NewsSource.InvalidSnap);
            }
        }
    }

    public void DetachCrate(IMovableSnappable detaching)
    {
        if ((object)detaching == LeftInput)
        {
            LeftInput = null;
        }
        else if ((object)detaching == RightInput)
        {
            RightInput = null;
        }
    }

    public override void Convert()
    {
    }

    public override void ClearSinks()
    {
    }

    public override ResourceContainerDictionary GetStartingDataContainers()
    {
        FlexData = new ThreeDPrinterFlexData()
        {
            Printing = Matter.Unspecified
        };
        return new ResourceContainerDictionary();
    }

    public override void Report()
    {
    }

    public override Module GetModuleType()
    {
        return Module.ThreeDPrinter;
    }

    public void OnEmergencyShutdown()
    {
    }
}
