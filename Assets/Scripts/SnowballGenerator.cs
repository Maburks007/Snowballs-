using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowballGenerator : MonoBehaviour
{
    private void Awake()
    {
        foreach (Transform snowb in transform)
            UpdateSnowball(snowb);
    }

    public void UpdateSnowball(Transform snowball)
    {
        //Gets the all active terrains in the scene
        Terrain[] terrains = Terrain.activeTerrains;

        //Will be the terrain that the snow is located in
        Terrain currentTerrain = null;

        Vector3 randomWorldPosition = new Vector3(Random.Range(0f, 0f), 0f, Random.Range(0f, 0f));

        foreach (Terrain terrain in terrains)
        {
            TerrainData terrainData = terrain.terrainData;

            // Adjust the random position based on the size of the current terrain
            randomWorldPosition.x = Random.Range(-118f, 118f);
            randomWorldPosition.z = Random.Range(-118f, 118f);

            // Check if the random position is within the bounds of this terrain
            if (randomWorldPosition.x >= terrain.transform.position.x &&
                randomWorldPosition.x <= terrain.transform.position.x + terrainData.size.x &&
                randomWorldPosition.z >= terrain.transform.position.z &&
                randomWorldPosition.z <= terrain.transform.position.z + terrainData.size.z)
            {
                currentTerrain = terrain;
                break;
            }
        }


        if (currentTerrain != null)
        {
            TerrainData terrainData = currentTerrain.terrainData;

            // Use GetInterpolatedHeight for floating-point coordinates
            float terrainHeight = terrainData.GetInterpolatedHeight(
                (randomWorldPosition.x - currentTerrain.transform.position.x) / terrainData.size.x,
                (randomWorldPosition.z - currentTerrain.transform.position.z) / terrainData.size.z
            );

            // Set the object's position based on the terrain height
            snowball.position = new Vector3(randomWorldPosition.x, terrainHeight, randomWorldPosition.z);
            snowball.GetComponent<Snow>().SetSize(Random.Range(1, 3) * 5);
        }
        else
        {
            UpdateSnowball(snowball);
        }
    }
}
