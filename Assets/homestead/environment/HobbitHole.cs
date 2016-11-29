using UnityEngine;
using System.Collections;
using System;

public class HobbitHole : MonoBehaviour {
    private const int BufferSize = 30;
    private const int XDiameter = 5;
    private const int YDiameter = 2;
    private const int ZDepth = 10;

    private class CavernMapClass
    {
        public Transform[,,] Transforms;
        public bool[,,] IsExcavated;

        public Transform this[Vector3 localPosition]
        {
            get
            {
                return this.Transforms[(int)localPosition.x + XDiameter, (int)localPosition.y + YDiameter, (int)localPosition.z];
            }
        }

    }

    public Transform cavernWallPrefab, cavernPrefab;

    private Transform[,,] CavernTransforms = new Transform[(2 * XDiameter) + 1, (2 * YDiameter) + 1, ZDepth];
    private bool[,,] CavernMap = new bool[(2 * XDiameter) + 1, (2 * YDiameter) + 1, ZDepth];
    
    private TransformBuffer CavernWallBuffer = new TransformBuffer();
    private TransformBuffer CavernBuffer = new TransformBuffer();

    private class TransformBuffer
    {
        public Transform Prefab, Parent;
        public int LastIndex = -1;
        public Transform[] Buffer = new Transform[BufferSize];

        public void AddNew()
        {
            Transform newT = GameObject.Instantiate<Transform>(Prefab);
            newT.parent = this.Parent;
            AddExisting(newT);
        }

        public void AddExisting(Transform t)
        {
            if (LastIndex < BufferSize - 1)
            {
                LastIndex++;
                Buffer[LastIndex] = t;
                t.gameObject.SetActive(false);
            }
            else
            {
                //todo: destroy instead
                t.gameObject.SetActive(false);
            }
        }

        public Transform Get(Vector3 localPosition)
        {
            if (LastIndex == -1)
            {
                AddNew();
            }

            Transform result = Buffer[LastIndex];

            LastIndex -= 1;

            result.localPosition = localPosition;
            result.gameObject.SetActive(true);

            return result;
        }
    }


    // Use this for initialization
    void Start () {
        CavernWallBuffer.Parent = CavernBuffer.Parent = this.transform;
        CavernWallBuffer.Prefab = cavernWallPrefab;
        CavernBuffer.Prefab = cavernPrefab;

        for (int i = 0; i < BufferSize / 2; i++)
        {
            CavernWallBuffer.AddNew();
            CavernBuffer.AddNew();
        }
    }

    // Update is called once per frame
    void Update () {
	
	}

    public void Excavate(Vector3 lPosition)
    {
        if (!CavernMap[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z])
        {
            Transform existingCavernWall = CavernTransforms[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z];

            CavernWallBuffer.AddExisting(existingCavernWall);

            CavernTransforms[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z] = CavernBuffer.Get(lPosition);

            CavernMap[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z] = true;

            FillAdjacentTransformless(lPosition);
        }
    }

    private void FillAdjacentTransformless(Vector3 fromLocalPosition)
    {
        Fill(fromLocalPosition + Vector3.left, true);
        Fill(fromLocalPosition + Vector3.right, true);
        Fill(fromLocalPosition + Vector3.forward, true);
        Fill(fromLocalPosition + Vector3.back, true);
        Fill(fromLocalPosition + Vector3.up, true);
        Fill(fromLocalPosition + Vector3.down, true);
    }

    public void Fill(Vector3 lPosition, bool onlyVoidCells = false)
    {
        Transform existingTransform = CavernTransforms[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z];
        bool isVoidCell = existingTransform == null;

        if (onlyVoidCells && !isVoidCell)
            return;

        bool isEmpty = CavernMap[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z];
        
        if (isEmpty)
        {
            if (existingTransform != null)
            {
                CavernBuffer.AddExisting(existingTransform);
            }
        }

        CavernTransforms[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z] = CavernWallBuffer.Get(lPosition);

        CavernMap[(int)lPosition.x, (int)lPosition.y, (int)lPosition.z] = false;
    }
}
