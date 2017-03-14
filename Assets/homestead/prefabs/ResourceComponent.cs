using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;
using RedHomestead.Persistence;

[Serializable]
public class CrateData : FacingData
{
    public Matter ResourceType;
    public float Quantity = 1;
}

[RequireComponent(typeof(Rigidbody))]
public class ResourceComponent : MonoBehaviour, IDataContainer<CrateData> {
    public CrateData data;
    public CrateData Data {
        get { return data;  }
        set { data = value; }
    }

    public AudioClip MetalBang;
    public ICrateSnapper SnappedTo;

    public Mesh[] ResourceLabelMeshes, CompoundLabelMeshes;
    public MeshFilter LabelMeshFilter;

    private Rigidbody myRigidbody;
    private Collider myCollider;

    //todo: could be CurrentConstructionZone reference instead
    internal bool IsInConstructionZone = false;
    internal bool IsOutsideConstructionZone
    {
        get
        {
            return !IsInConstructionZone;
        }
    }


    void Start()
    {
        myRigidbody = GetComponent<Rigidbody>();
        myCollider = GetComponent<Collider>();
        RefreshLabel();
    }

    public void RefreshLabel()
    {
        if (Data.ResourceType != Matter.Unspecified)
        {
            int index = (int)Data.ResourceType;

            if (index > 0)
                LabelMeshFilter.mesh = ResourceLabelMeshes[index - 1];
            else
                LabelMeshFilter.mesh = CompoundLabelMeshes[index + 6];
        }
    }

    private FixedJoint parentJoint;

    public void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition, Rigidbody jointRigid = null)
    {
        PlayerInput.Instance.DropObject();
        this.SnappedTo = snapParent;
        transform.position = snapPosition;
#warning snap crate rotation does not inherit from trigger forwarder
        transform.rotation = snapParent.transform.rotation;

        if (jointRigid != null)
        {
            myRigidbody.velocity = Vector3.zero;
            parentJoint = gameObject.AddComponent<FixedJoint>();
            parentJoint.connectedBody = jointRigid;
            parentJoint.breakForce = Mathf.Infinity;
            parentJoint.breakTorque = Mathf.Infinity;
            parentJoint.enableCollision = false;
        }
        else
        {
            myRigidbody.useGravity = false;
            myRigidbody.isKinematic = true;
        }


        PlayerInput.Instance.PlayInteractionClip(snapPosition, MetalBang);
    }

    //called in pick up object
    public void UnsnapCrate()
    {
        this.SnappedTo.DetachCrate(this);
        this.SnappedTo = null;

        if (this.parentJoint != null)
        {
            GameObject.Destroy(parentJoint);
        }

        myRigidbody.isKinematic = false;
        myRigidbody.useGravity = true;
        PlayerInput.Instance.PlayInteractionClip(transform.position, MetalBang);
    }

    internal string GetText()
    {
        return this.Data.ResourceType.ToString() + String.Format(" {0:0.##}kg", this.Data.Quantity * this.Data.ResourceType.Kilograms());
    }
}
