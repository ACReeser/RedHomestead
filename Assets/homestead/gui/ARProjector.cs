using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ARProjector : MonoBehaviour {
    public Transform follow;
    public Transform canvas;
    public Vector3 followOffset;
    private Mesh mesh;

    int[] centerIndices = new int[8];
    private Vector3[] originalVerts;
    private bool projecting;
    private Vector3 myOriginalScale, canvasOriginalScale;

    void Start()
    {
        this.myOriginalScale = this.transform.localScale;
        this.canvasOriginalScale = canvas.localScale;
        this.transform.localScale = canvas.localScale = Vector3.zero;

        this.mesh = this.GetComponent<MeshFilter>().mesh;

        this.originalVerts = this.mesh.vertices;
        var centerI = 0;
        for (int i = 0; i < originalVerts.Length; i++)
        {
            var v = originalVerts[i];
            print(v);
            if (v.x == 0f && v.z == 0f)
            {
                centerIndices[centerI] = i;
                centerI++;
            }
        }
    }

	// Update is called once per frame
	void Update () {
        if (projecting)
        {
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

        if (Input.GetKeyUp(KeyCode.R))
        {
            StartCoroutine(changeProjector(!this.projecting));
        }
    }

    private IEnumerator changeProjector(bool newState)
    {
        float duration = .8f;
        float time = 0f;
        Vector3 myFrom, myTo, canvasFrom, canvasTo;
        if (newState)
        {
            canvasFrom = myFrom = Vector3.zero;
            canvasTo = canvasOriginalScale;
            myTo = myOriginalScale;
            projecting = true;
        }
        else
        {

            canvasTo = myTo = Vector3.zero;
            canvasFrom = canvasOriginalScale;
            myFrom = myOriginalScale;
        }

        while (time < duration)
        {
            this.transform.localScale = Vector3.Lerp(myFrom, myTo, time/duration);
            this.canvas.localScale = Vector3.Lerp(canvasFrom, canvasTo, time/duration);
            yield return null;
            time += Time.deltaTime;
        }

        this.transform.localScale = myTo;
        this.canvas.localScale = canvasTo;

        this.projecting = newState;
    }
}
