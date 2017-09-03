using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARProjector : MonoBehaviour {
    public Transform follow;
    public Vector3 followOffset;
    private Mesh mesh;

    int[] centerIndices = new int[8];
    private Vector3[] originalVerts;

    void Start()
    {
        this.mesh = this.GetComponent<MeshFilter>().mesh;

        this.originalVerts = this.mesh.vertices;
        var centerI = 0;
        for (int i = 0; i < originalVerts.Length; i++)
        {
            var v = originalVerts[i];
            if (v.x == 0f && v.z == 0f)
            {
                centerIndices[centerI] = i;
                centerI++;
            }
        }
    }

	// Update is called once per frame
	void Update () {
        Vector3 newCenter = this.transform.InverseTransformPoint(this.follow.TransformPoint(followOffset));
        Vector3[] newVerts = this.originalVerts;
        newVerts[centerIndices[0]] = newCenter;
        newVerts[centerIndices[1]] = newCenter;
        newVerts[centerIndices[2]] = newCenter;
        newVerts[centerIndices[3]] = newCenter;
        newVerts[centerIndices[4]] = newCenter;
        newVerts[centerIndices[5]] = newCenter;
        newVerts[centerIndices[6]] = newCenter;
        newVerts[centerIndices[7]] = newCenter;
        this.mesh.vertices = newVerts;
        this.mesh.RecalculateBounds();
        this.mesh.RecalculateNormals();
    }
}
