using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.AI;

public class AISnowball : MonoBehaviour
{


    public float decayValue;

    public float maxMoveSpeed;
    public float currentMoveSpeed;
    public float slope;

    public float maxRotateSpeed;
    float currentRotateSpeed;

    public float sizeSlopePenalty = 0;
    public float speedPenalty = 1;

    [SerializeField] int snowballSize;
    public int GetSize
    {
        get { return snowballSize; }
    }

    public float rotateSpeed;

    public Rigidbody myRigid;

    public NavMeshAgent myAgent;

    Terrain[] terrains;

    public TextMeshProUGUI sizeText;

    public SphereCollider myCollider;

    GameObject[] snowballers;
    public void NewSnowballer()
    {
        snowballers = GetListOfSnowballers(GameObject.FindGameObjectsWithTag("Snowballer"));
    }

    public Transform sizeCanvas;
    public Transform body;
    public Transform character;
    public Transform snowball;

    public Transform snowGenerator;

    Transform currentSnowball;
    Transform currentTarget;

    int currentWaypointIndex = 0;

    private void Start()
    {
        snowballers = GetListOfSnowballers(GameObject.FindGameObjectsWithTag("Snowballer"));

        myAgent = GetComponent<NavMeshAgent>();

        currentMoveSpeed = maxMoveSpeed;
        currentRotateSpeed = maxRotateSpeed;

        snowGenerator = GameObject.Find("Snow Generator").transform;
        terrains = Terrain.activeTerrains;

        SpawnSnowballer();
        AddSnow(RandomSize());

        SetNewPath(GetSnow());
    }

    [SerializeField] int sizeStartMultiplier;
    int RandomSize()
    {
        int randomStartSize = Random.Range(0, 100);

        if(randomStartSize >= 0 && randomStartSize <= 70)
        {
            return 10 * sizeStartMultiplier;
        }
        else if (randomStartSize >= 71 && randomStartSize <= 80)
        {
            return ((int)Random.Range(10, 14) * 5) * sizeStartMultiplier;
        }
        else if (randomStartSize >= 81 && randomStartSize <= 91)
        {
            return ((int)Random.Range(12, 17) * 5) * sizeStartMultiplier;
        }
        else if (randomStartSize >= 91 && randomStartSize <= 95)
        {
            return ((int)Random.Range(18, 20) * 5) * sizeStartMultiplier;
        }
        else if (randomStartSize >= 96)
        {
            return ((int)Random.Range(21, 25) * 5) * sizeStartMultiplier;
        }


        return 10;
    }

    GameObject[] GetListOfSnowballers(GameObject[] objects)
    {
        // Filter out the current game object from the list
        int index = System.Array.IndexOf(objects, gameObject);
        if (index != -1)
        {
            GameObject[] filteredObjects = new GameObject[objects.Length - 1];
            System.Array.Copy(objects, 0, filteredObjects, 0, index);
            System.Array.Copy(objects, index + 1, filteredObjects, index, objects.Length - index - 1);
            return filteredObjects;
        }
        else
        {
            return objects;
        }
    }

    bool checkingPathTime = false;
    bool stopMoving = false;
    bool attackingTarget = false;
    bool runningAway = false;

    private void Update()
    {


        if (Camera.main != null)
            sizeCanvas.LookAt(Camera.main.transform);

        RotateCharacter();
        GetSlope();

        snowball.Rotate(Vector3.right * myRigid.velocity.magnitude * currentRotateSpeed * 0.25f);

        if (currentWaypointIndex < myAgent.path.corners.Length)
        {
            if (Vector3.Distance(transform.position, myAgent.path.corners[currentWaypointIndex]) < 2f)
            {
               SetNextWaypoint();
            }
        }
        else
            SetNewPath(GetSnow());


        if (myAgent.path != null)
        {
            MoveAlongPath();
            DrawAgentPath();

            if (CheckForPredator())
            {
                SetNewPath(FurthestSnow());
                runningAway = true;
            }
            else {
                runningAway = false;
                
                if (CheckForTarget())
                {
                    SetNewPath(currentTarget.position);
                }
                else {
                    if (SnowballInactive())
                    {
                        SetNewPath(GetSnow());
                    }
                }
            }

            if (Vector3.Distance(transform.position, pathEndPoint) <= 15 && !checkingPathTime && !runningAway)
            {
                StartCoroutine(CheckPathTime());
            }
        }
    }

    bool CheckForPredator()
    {
        if (GameObject.Find("Player") != null)
            return IsSmaller(GameObject.Find("Player").transform) && Vector3.Distance(GameObject.Find("Player").transform.position, transform.position) <= 12;
        else
            return false;
    }


    bool CheckForTarget()
    {
        foreach (GameObject snowballer in snowballers)
        {
            if (snowballer == null)
                break;

            Vector3 snowballerPos = snowballer.transform.position;

            if (Vector3.Distance(snowballerPos, transform.position) < 20 && IsBigger(snowballer.transform))
            {
                currentTarget = snowballer.transform;
                return true;
            }
        }

        return false;
    }

    bool IsSmaller(Transform target)
    {
        int targetSize = 0;

        SnowBallStats playerScript = target.GetComponent<SnowballMovement>().SBS;
        if (playerScript != null)
            targetSize = playerScript.GetSize;

        ThrownSnoball thrownBallScript = target.GetComponent<ThrownSnoball>();
        if (thrownBallScript != null)
            targetSize = thrownBallScript.GetSize;

        return ((snowballSize * 0.3f) + snowballSize) < targetSize;
    }

    bool IsBigger(Transform target)
    {
        int targetSize = 0;

        SnowBallStats playerScript = target.GetComponent<SnowBallStats>();
        if (playerScript != null)
            targetSize = playerScript.GetSize;

        AISnowball aiScript = target.GetComponent<AISnowball>();
        if (aiScript != null)
            targetSize = aiScript.GetSize;

        ThrownSnoball thrownBallScript = target.GetComponent<ThrownSnoball>();
        if (thrownBallScript != null)
            targetSize = thrownBallScript.GetSize;

       return ((targetSize * 0.3f) + targetSize) < snowballSize;
    }

    IEnumerator CheckPathTime()
    {
        checkingPathTime = true;
        yield return new WaitForSeconds(Random.Range(5f, 8f));

        //Checks to see if the distance between AI and end point is still lower than 5
        if (Vector3.Distance(transform.position, pathEndPoint) <= 15)
        {
            stopMoving = true;
            myRigid.velocity = Vector3.zero;

            yield return new WaitForSeconds(Random.Range(0.25f, 3.25f));

            stopMoving = false;
        }

        checkingPathTime = false;
    }

    #region AI Path

    Vector3 pathEndPoint;
    void SetNewPath(Vector3 path)
    {
        pathEndPoint = path;

        NavMeshPath newPath = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, pathEndPoint, NavMesh.AllAreas, newPath);
        myAgent.SetPath(newPath);

        currentWaypointIndex = 0;
    }


    void SetNextWaypoint()
    {
        currentWaypointIndex++;


        if (currentWaypointIndex >= myAgent.path.corners.Length)
        {
            SetNewPath(GetSnow());
        }
    }
    void MoveAlongPath()
    {
        if(myAgent.path != null && myAgent.path.corners.Length > 0)
        {
            Vector3 targetPos = myAgent.path.corners[currentWaypointIndex];

            if(!stopMoving)
                myRigid.velocity = transform.forward * currentMoveSpeed * speedPenalty;


            Vector3 directionToTarget = (targetPos - transform.position);

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotateSpeed * Time.deltaTime);
        }
    }

    #endregion


    bool SnowballInactive()
    {
        return !currentSnowball.position.Equals(pathEndPoint);
    }

    RaycastHit hit;
    public Transform forwardTrasnform;
    void GetSlope()
    {
        Physics.SphereCast(transform.position, 0.25f, -Vector3.up, out hit, 0.85f);

        Vector3 moveDir = (forwardTrasnform.position - transform.position).normalized;

        slope = moveDir.y;
        
        speedPenalty = Mathf.Clamp(1 - (slope + (slope * sizeSlopePenalty)), 0, 2);
    }

    void RotateCharacter()
    {
        Terrain currentTerrain = null;

        if (terrains == null)
        {
            Debug.LogError("Terrain not assigned!");
            return;
        }

        //Checks which terrain the object is in
        foreach (Terrain t in terrains)
        {
            TerrainData tData = t.terrainData;

            if (transform.position.x >= t.transform.position.x &&
               transform.position.x <= t.transform.position.x + tData.size.x &&
               transform.position.z >= t.transform.position.z &&
               transform.position.z <= t.transform.position.z + tData.size.z)
            {
                currentTerrain = t;
                break;
            }
        }


        if (currentTerrain != null)
        {
            //Get the position of the game object in the terrain's local space
            Vector3 terrainLocalPos = currentTerrain.transform.InverseTransformPoint(transform.position);

            //Get the terrain normal at the position of the object
            Vector3 terrainNormal = currentTerrain.terrainData.GetInterpolatedNormal(terrainLocalPos.x / currentTerrain.terrainData.size.x,
                                                                              terrainLocalPos.z / currentTerrain.terrainData.size.z);

            //Calculate the rotation based on the terrain normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, terrainNormal) * transform.rotation;

            //Smoothly rotate the object towards the target rotation
            character.rotation = Quaternion.Lerp(character.rotation, targetRotation, 5 * Time.deltaTime);
        }
    }

    Vector3 FurthestSnow()
    {
        currentSnowball = snowGenerator.GetChild(0);
        foreach (Transform snowball in snowGenerator)
        {
            float currentDistance = Vector3.Distance(currentSnowball.position, transform.position);
            float newDistance = Vector3.Distance(snowball.position, transform.position);

            if (currentDistance < newDistance)
            {
                currentSnowball = snowball;
            }
        }


        return currentSnowball.position;
    }

    Vector3 GetSnow()
    {
        currentSnowball = snowGenerator.GetChild(0);
        foreach(Transform snowball in snowGenerator)
        {
            float currentDistance = Vector3.Distance(currentSnowball.position, transform.position);
            float newDistance = Vector3.Distance(snowball.position, transform.position);

            if (currentDistance > newDistance)
            {
                currentSnowball = snowball;
            }
        }


        return currentSnowball.position;
    }

    void DrawAgentPath()
    {


        for (int i = 1; i < myAgent.path.corners.Length; i++)
        {
            Debug.DrawLine(myAgent.path.corners[i - 1], myAgent.path.corners[i], Color.red);
        }
    }


    public void AddSnow(int snowAmount)
    {
        snowballSize += snowAmount;

        snowball.localScale = Vector3.one * (1 + (snowballSize * 0.00625f));
        snowball.localPosition = new Vector3(0, (snowball.localScale.y / 2f) - 0.5f, 0);

        body.localPosition = new Vector3(0, 0, -1 - (((snowballSize * 0.005f)) / 2f));

       

      //  myCollider.center = snowball.localPosition;
      //  myCollider.radius = snowball.localScale.x / 2f;

        sizeCanvas.localPosition = new Vector3(sizeCanvas.localPosition.x, 1.4f + (snowballSize * 0.0075f), sizeCanvas.localPosition.z);
        sizeText.fontSize = 0.5f * (1 + (snowballSize * 0.002f));
        sizeText.text = snowballSize.ToString();


        currentMoveSpeed = maxMoveSpeed * ((Mathf.Exp((-1 / decayValue) * snowballSize)));
        currentRotateSpeed = maxRotateSpeed * Mathf.Exp((-1 / decayValue) * snowballSize * 2f);

      //  sizeSlopePenalty = (Mathf.Exp((-1 / decayValue) * snowballSize) * 0.9f);
    }

    public void Swallowed()
    {
        snowballSize = 0;    
        AddSnow(10);

        SpawnSnowballer();
    }

    void SpawnSnowballer() 
    {
        Vector3 randomWorldPosition = new Vector2();
        Terrain currentTerrain = null;

        foreach (Terrain terrain in terrains)
        {
            TerrainData terrainData = terrain.terrainData;

            // Adjust the random position based on the size of the current terrain
            randomWorldPosition.x = Random.Range(-102f, 102f);
            randomWorldPosition.z = Random.Range(-102f, 102f);

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
            transform.position = new Vector3(randomWorldPosition.x, terrainHeight, randomWorldPosition.z);
        }
        else
        {
            SpawnSnowballer();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Snow"))
        {
            AddSnow(other.GetComponent<Snow>().Size);

            SetNewPath(GetSnow());

            other.transform.parent.SendMessage("UpdateSnowball", other.transform);

            if(GameObject.Find("Score Board Canvas") != null)
            GameObject.Find("Score Board Canvas").SendMessage("UpdateList");

        }
        else if (other.tag.Equals("Snowballer"))
        {
            SnowBallStats player = other.GetComponent<SnowBallStats>();
            if (player != null)
                return;


            AISnowball otherScript = other.GetComponent<AISnowball>();
            ThrownSnoball thrownBallScript = other.GetComponent<ThrownSnoball>();

            int targetSize = 0;

            if (otherScript != null) 
                targetSize = otherScript.GetSize;

            if (thrownBallScript != null)
                targetSize = thrownBallScript.GetSize;


                if (targetSize + (targetSize * 0.3f) < snowballSize)
                {
                    AddSnow(targetSize);

                    other.SendMessage("Swallowed");

                    if(GameObject.Find("Score Board Canvas") != null)
                    GameObject.Find("Score Board Canvas").SendMessage("UpdateList");
                }
        }
    }
}
