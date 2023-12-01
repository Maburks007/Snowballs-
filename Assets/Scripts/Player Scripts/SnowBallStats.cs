using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SnowBallStats : MonoBehaviour
{
    public Transform sizeCanvas;
    public TextMeshProUGUI sizeText;

    public RawImage blackoutImage;

    public Transform player;
    public Transform snowball;
    public Transform character;
    public Transform cameraPlacement;

    public SphereCollider myCollider;
    public SnowballMovement SBM;


    [SerializeField] int snowballSize;
    public int GetSize
    {
        get { return snowballSize; }
    }



    void Start()
    {
        SpawnPlayer();
    }

    float cameraZ = -1.3f;
    float cameraY = 2.9f;
    // Update is called once per frame
    void Update()
    {
        LerpCameraToPos();

        sizeCanvas.LookAt(Camera.main.transform);
    }

    float prevCameraY;
    float prevCameraZ;   
   public void AddSnow(int snowAmount)
    {
        snowballSize += snowAmount;

        snowball.localScale = Vector3.one * (1 + (snowballSize * 0.00625f));
        snowball.localPosition = new Vector3(0, (snowball.localScale.y /2f) - 0.5f, 0);

        character.localPosition = new Vector3(0,0,  -1 - (((snowballSize * 0.005f)) / 2f));

        if (finishedLerping)
        {
            prevCameraY = cameraY;
            prevCameraZ = cameraZ;
            currentLerpDuration += lerpDuration;
        }
        else
            currentLerpDuration += lerpDuration * 0.35f;

        cameraY = 2.9f + (snowballSize * 0.0085f);
        cameraZ = -1.3f - (snowballSize * 0.0085f);


        sizeCanvas.localPosition = new Vector3(sizeCanvas.localPosition.x, 1.4f + (snowballSize * 0.0075f), sizeCanvas.localPosition.z);
        sizeText.fontSize = 0.5f * (1 + (snowballSize * 0.002f));
        sizeText.text = snowballSize.ToString();
            
        SBM.UpdateSnowBall(snowballSize);

       GameObject.Find("Score Board Canvas").SendMessage("UpdateList");
    }



    float currentCameraZ = -1.3f;
    float currentCameraY = 2.9f;

    public float lerpDuration;
    public float currentLerpDuration;
    float timeElapsed;

    bool finishedLerping = true;
    void LerpCameraToPos()
    {
        if (currentCameraY != cameraY || currentCameraZ != cameraZ) {
            if (timeElapsed < currentLerpDuration)
            {
                finishedLerping = false;
                currentCameraY = Mathf.Lerp(prevCameraY, cameraY, (timeElapsed / currentLerpDuration));
                currentCameraZ = Mathf.Lerp(prevCameraZ, cameraZ, (timeElapsed / currentLerpDuration));
                timeElapsed += Time.deltaTime;
            }
            else
            {          
                currentCameraY = cameraY;
                currentCameraZ = cameraZ;

                timeElapsed = 0;
                currentLerpDuration = 0;
                finishedLerping = true;
            }
        }

         cameraPlacement.localPosition = new Vector3(0, currentCameraY, currentCameraZ);
    }


    #region Death
    public void Swallowed()
    {
        SBM.SetPlayerReset = true;

        StartCoroutine(Blackout());
    }

    public float blackoutDuration;
    IEnumerator Blackout()
    {
        float elapsed_time = 0f;
        Color startColor = blackoutImage.color;
        Color targetColor = new Color(startColor.r, startColor.g, startColor.b, 255f);

        while (elapsed_time < blackoutDuration)
        {
           blackoutImage.color = Color.Lerp(startColor, targetColor, elapsed_time / (blackoutDuration) * Time.deltaTime);

            elapsed_time += Time.deltaTime;

            yield return null;
        }
        blackoutImage.color = targetColor;

        yield return new WaitForSeconds(0.75f);

        snowballSize = 0;
        AddSnow(10);



        startColor = blackoutImage.color;
        targetColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        SpawnPlayer();

        elapsed_time = 0;

        while (elapsed_time < blackoutDuration)
        {
            blackoutImage.color = Color.Lerp(startColor, targetColor, elapsed_time / (blackoutDuration));

            elapsed_time += Time.deltaTime;

            yield return null;
        }
        blackoutImage.color = targetColor;
    }

    void SpawnPlayer()
    {

        SBM.SetPlayerReset = false;

        //Gets the all active terrains in the scene
        Terrain[] terrains = Terrain.activeTerrains;

        //Will be the terrain that the snow is located in
        Terrain currentTerrain = null;

        Vector3 randomWorldPosition = new Vector3(Random.Range(0f, 0f), 0f, Random.Range(0f, 0f));

        foreach (Terrain terrain in terrains)
        {
            TerrainData terrainData = terrain.terrainData;

            // Adjust the random position based on the size of the current terrain
            randomWorldPosition.x = Random.Range(-91f, 91f);
            randomWorldPosition.z = Random.Range(-80f, 89f);

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
            player.position = new Vector3(randomWorldPosition.x, terrainHeight, randomWorldPosition.z);
        }
        else
        {
            SpawnPlayer();
        }
    }

    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Snow"))
        {
            AddSnow(other.GetComponent<Snow>().Size);

            other.transform.parent.SendMessage("UpdateSnowball", other.transform);

            GameObject.Find("Score Board Canvas").SendMessage("UpdateList");
        }
        else if (other.tag.Equals("Snowballer"))
        {
            int targetSize = 0;

            AISnowball aiScript = other.GetComponent<AISnowball>();

            if(aiScript != null)
                 targetSize = aiScript.GetSize;

            ThrownSnoball throwSnowballScript = other.GetComponent<ThrownSnoball>();

            if (throwSnowballScript != null)
                targetSize = throwSnowballScript.GetSize;


            if(targetSize + (targetSize * 0.3f) < snowballSize)
            {
                AddSnow(targetSize);

                other.SendMessage("Swallowed");

            }
            else if (snowballSize + (snowballSize * 0.3f) < targetSize)
            {
                if (throwSnowballScript != null)
                    throwSnowballScript.AddSnow(snowballSize);

                Swallowed();
            }

            SBM.ThrownBallUpdater();
        }
    }
}
