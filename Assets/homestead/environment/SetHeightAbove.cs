using UnityEngine;
using System.Collections;
using System;

public class SetHeightAbove : MonoBehaviour {
    private Terrain myTerrain;

	// Use this for initialization
	void Start () {
        myTerrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        SetHeight();
	}

    private void SetHeight()
    {
        //Vector3 terrainPos = GetRelativeTerrainPositionFromPos(this.transform.position, myTerrain, myTerrain.terrainData.heightmapWidth, myTerrain.terrainData.heightmapHeight);
        
        //myTerrain.terrainData.SetHeights(terrainPos.x, terrainPos.z, alphas);
    }

    protected Vector3 GetRelativeTerrainPositionFromPos(Vector3 pos, Terrain terrain, int mapWidth, int mapHeight)
    {
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(pos, terrain);
        return new Vector3((coord.x * mapWidth), 0, (coord.z * mapHeight));
    }

    protected Vector3 GetNormalizedPositionRelativeToTerrain(Vector3 pos, Terrain terrain)
    {
        Vector3 tempCoord = (pos - terrain.gameObject.transform.position);
        Vector3 coord;
        coord.x = tempCoord.x / myTerrain.terrainData.size.x;
        coord.y = tempCoord.y / myTerrain.terrainData.size.y;
        coord.z = tempCoord.z / myTerrain.terrainData.size.z;
        return coord;
    }
}
