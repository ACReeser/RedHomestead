using RedHomestead.Crafting;
using RedHomestead.Simulation;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeDPrinter : MonoBehaviour, IDoorManager, ITriggerSubscriber, ICrateSnapper
{
    public Transform printArm, printHead;

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

    // Use this for initialization
    void Start () {
        resetPosition = printArm.localPosition;
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Material cutawayMaterial;
    private MeshRenderer currentPrintRenderer;
    public Transform InProgressPrintCrate;
    internal void ToggleArmPrint()
    {
        if (currentPrint != null)
            StopCoroutine(currentPrint);

        //InProgressPrintCrate = GameObject.Instantiate(FloorplanBridge.Instance.CraftableFields.Prefabs[Convert.ToInt32(Craftable.Crate)], this.transform.TransformPoint(crateStartLocalPosition), Quaternion.identity);
        //InProgressPrintCrate.GetComponent<Collider>().enabled = false;
        //InProgressPrintCrate.GetComponent<Rigidbody>().isKinematic = true;
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

        while (!armY.Done)
        {
            armY.Tick(printArm);
            headX.Tick(printHead);
            printArm.localPosition = new Vector3(printArm.localPosition.x, printArm.localPosition.y, back ? Mathf.Lerp(minArmZ, maxArmZ, headX.T) : Mathf.Lerp(maxArmZ, minArmZ, headX.T));

            currentPrintRenderer.material.SetFloat("_showPercentY", armY.T * 100f);
            
            yield return null;

            if (headX.Done)
            {
                headX = GetNextHead();
                back = !back;
            }
        }

        currentPrintRenderer.enabled = false;
        GameObject.Instantiate(FloorplanBridge.Instance.CraftableFields.Prefabs[Convert.ToInt32(Craftable.Crate)], this.transform.TransformPoint(crateStartLocalPosition), Quaternion.identity);

        MainMenu.LerpContext reset = new MainMenu.LerpContext()
        {
            Space = Space.Self
        };
        reset.Seed(printArm.localPosition, resetPosition, 2f);
        while(!reset.Done)
        {
            reset.Tick(printArm);
            yield return null;
        }
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
}
