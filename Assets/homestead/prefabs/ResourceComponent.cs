using UnityEngine;
using System.Collections;
using RedHomestead.Simulation;
using System;

[RequireComponent(typeof(Rigidbody))]
public class ResourceComponent : MonoBehaviour {
    public Matter ResourceType;
    public float Quantity = 1;
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
        if (ResourceType != Matter.Unspecified)
        {
            int index = (int)ResourceType;

            if (index > 0)
                LabelMeshFilter.mesh = ResourceLabelMeshes[index - 1];
            else
                LabelMeshFilter.mesh = CompoundLabelMeshes[index + 6];
        }
    }

    //void OnCollisionEnter(Collision c)
    //{

    //}

    public void SnapCrate(ICrateSnapper snapParent, Vector3 snapPosition)
    {
        PlayerInput.Instance.DropObject();
        this.SnappedTo = snapParent;
        myRigidbody.isKinematic = true;
        myRigidbody.useGravity = false;
        transform.position = snapPosition;
        transform.localRotation = Quaternion.identity;
        PlayerInput.Instance.PlayInteractionClip(snapPosition, MetalBang);
    }

    //called in pick up object
    public void UnsnapCrate()
    {
        this.SnappedTo.DetachCrate(this);
        this.SnappedTo = null;
        myRigidbody.isKinematic = false;
        myRigidbody.useGravity = true;
        PlayerInput.Instance.PlayInteractionClip(transform.position, MetalBang);
    }

    internal string GetText()
    {
        return this.ResourceType.ToString() + String.Format(" {0:0.##}kg", this.Quantity * this.ResourceType.Kilograms());
    }
}
