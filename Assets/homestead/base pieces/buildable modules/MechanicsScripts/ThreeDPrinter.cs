using RedHomestead.Buildings;
using RedHomestead.Crafting;
using RedHomestead.Electricity;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeDPrinter : Converter, IDoorManager, ITriggerSubscriber, ICrateSnapper, IPowerConsumerToggleable
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
    }

    public Material cutawayMaterial;
    private MeshRenderer currentPrintRenderer;
    public Transform InProgressPrintCrate;
    internal void ToggleArmPrint()
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
        armY.Seed(Vector3.up * minArmY, Vector3.up * maxArmY, 60f);
        bool back = false;
        laser.enabled = true;

        while (!armY.Done)
        {
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
        GameObject.Instantiate(FloorplanBridge.Instance.CraftableFields.Prefabs[System.Convert.ToInt32(Craftable.Crate)], this.transform.TransformPoint(crateStartLocalPosition), Quaternion.identity);

        MainMenu.LerpContext reset = new MainMenu.LerpContext()
        {
            Space = Space.Self
        };
        reset.Seed(printArm.localPosition, resetPosition, 2f);
        while(!reset.Done)
        {
            reset.Tick(printArm);
            printArmHarness.localPosition = new Vector3(0f, 0f, printArm.localPosition.z);
            yield return null;
        }
    }

    private void UpdateLaser()
    {
        laser.SetPosition(0, laserAnchor.position);
        laser.SetPosition(1, new Vector3(laserAnchor.position.x, laserAnchor.position.y, laserMidpoint.position.z));
        laser.SetPosition(2, laserMidpoint.position);
        laser.SetPosition(3, new Vector3(printHead.position.x, laserMidpoint.position.y, laserMidpoint.position.z));
        laser.SetPosition(4, new Vector3(printHead.position.x, transform.position.y, printHead.position.z));
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
    private ResourceComponent leftInput, rightInput;

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
                if (child == leftInputTrigger && leftInput == null)
                {
                    leftInput = resComp;
                    res.SnapCrate(this, c.transform.position);
                }
                else if (child == rightInputTrigger && rightInput == null)
                {
                    rightInput = resComp;
                    res.SnapCrate(this, c.transform.position);
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
        if ((object)detaching == leftInput)
        {
            leftInput = null;
        }
        else if ((object)detaching == rightInput)
        {
            rightInput = null;
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
