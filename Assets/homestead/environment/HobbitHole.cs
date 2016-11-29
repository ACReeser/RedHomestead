using UnityEngine;
using System.Collections;
using System;

public class HobbitHole : MonoBehaviour {
    private const int BufferSize = 30;
    private const int XDiameter = 5;
    private const int YDiameter = 2;
    private const int ZDepth = 10;

    private class OffsetArray<T>
    {
        private int XOffset = 5;
        private int YOffset = 2;
        private int ZOffset = 0;

        public T[,,] Array;

        public OffsetArray(int xSize, int ySize, int zSize)
        {
            Array = new T[xSize, ySize, zSize];
        }

        public T this[Vector3 localPosition]
        {
            get
            {
                return this.Array[(int)localPosition.x + XOffset, (int)localPosition.y + YOffset, (int)localPosition.z + ZOffset];
            }
            set
            {
                this.Array[(int)localPosition.x + XOffset, (int)localPosition.y + YOffset, (int)localPosition.z + ZOffset] = value;
            }
        }
    }

    public Transform cavernWallPrefab, cavernPrefab, originWall;

    private OffsetArray<Transform> CavernTransforms = new OffsetArray<Transform>((2 * XDiameter) + 1, (2 * YDiameter) + 1, ZDepth);
    private OffsetArray<bool> CavernMap = new OffsetArray<bool>((2 * XDiameter) + 1, (2 * YDiameter) + 1, ZDepth);
    
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
            newT.localScale = Vector3.one;
            newT.localRotation = Quaternion.identity;
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

        CavernTransforms[Vector3.zero] = originWall;
    }

    // Update is called once per frame
    void Update () {
	
	}

    public void Excavate(Vector3 lPosition)
    {
        if (!CavernMap[lPosition])
        {
            Transform existingCavernWall = CavernTransforms[lPosition];

            CavernWallBuffer.AddExisting(existingCavernWall);

            CavernTransforms[lPosition] = CavernBuffer.Get(lPosition);

            CavernMap[lPosition] = true;

            FillAdjacentTransformless(lPosition);
        }
    }

    private void FillAdjacentTransformless(Vector3 fromLocalPosition)
    {
        Fill(fromLocalPosition + Vector3.left, true);
        Fill(fromLocalPosition + Vector3.right, true);
        Fill(fromLocalPosition + Vector3.forward, true);

        if (fromLocalPosition.z > 0)
            Fill(fromLocalPosition + Vector3.back, true);

        Fill(fromLocalPosition + Vector3.up, true);
        Fill(fromLocalPosition + Vector3.down, true);
    }

    public void Fill(Vector3 lPosition, bool onlyVoidCells = false)
    {
        Transform existingTransform = CavernTransforms[lPosition];
        bool isVoidCell = existingTransform == null;

        if (onlyVoidCells && !isVoidCell)
            return;

        bool isEmpty = CavernMap[lPosition];
        
        if (isEmpty)
        {
            if (existingTransform != null)
            {
                CavernBuffer.AddExisting(existingTransform);
            }
        }

        CavernTransforms[lPosition] = CavernWallBuffer.Get(lPosition);

        CavernMap[lPosition] = false;
    }
}
