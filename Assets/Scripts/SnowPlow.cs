using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class SnowPlow : MonoBehaviour
{
    public int maxSize;
    [SerializeField] int size = 0;

    public TextMeshProUGUI eatenText;

    public Transform trunk;
    public Transform eatenCanvas;

    Terrain[] terrains;

    public NavMeshAgent myAgent;

    public Rigidbody myRigid;

    public float currentMoveSpeed;
    public float currentRotateSpeed;

    GameObject[] snowballers;
    public void NewSnowballer()
    {
        snowballers = GameObject.FindGameObjectsWithTag("Snowballer");
    }


    // Start is called before the first frame update
    void Start()
    {
        NewSnowballer();

        snowballers = GameObject.FindGameObjectsWithTag("Snowballer");
        terrains = Terrain.activeTerrains;

        SetNewPath(NearestSnowballer());
    }

    // Update is called once per frame
    void Update()
    {
        RotateCharacter();
        GetMoveDirection();
        MoveAlongPath();

        if(Camera.main != null)
            eatenCanvas.LookAt(Camera.main.transform);

        if (currentWaypointIndex < myAgent.path.corners.Length)
        {
            if (Vector3.Distance(transform.position, myAgent.path.corners[currentWaypointIndex]) <= 5f)
            {
                SetNextWaypoint();
            }
            else if (Vector3.Distance(transform.position, myAgent.path.corners[myAgent.path.corners.Length - 1]) <= 10f && returningToDeposit)
            {
                DepositSnow();
                SetNextWaypoint();
            }
        }

        DrawAgentPath();
    }


    Vector3 NearestSnowballer()
    {
        Vector3 closestPath = snowballers[0].transform.position;

        foreach (GameObject snowballer in snowballers)
        {
            if (snowballer == null)
                break;

            Vector3 tempPos = snowballer.transform.position;

            if (Vector3.Distance(tempPos, transform.position) < Vector3.Distance(closestPath, transform.position))
            {
                closestPath = tempPos;
            }

        }

        return closestPath;
    }

    #region Ai path
    int currentWaypointIndex = 0;
    void SetNewPath(Vector3 path)
    {
        NavMeshPath newPath = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, path, NavMesh.AllAreas, newPath);
        myAgent.SetPath(newPath);

        currentWaypointIndex = 0;
    }

    void SetNextWaypoint()
    {
        currentWaypointIndex++;


        if (currentWaypointIndex >= myAgent.path.corners.Length)
        {
            SetNewPath(NearestSnowballer());
        }
    }
    void MoveAlongPath()
    {
        if (myAgent.path != null && myAgent.path.corners.Length > 0 && currentWaypointIndex < myAgent.path.corners.Length)
        {

            Vector3 targetPos = myAgent.path.corners[currentWaypointIndex];

                myRigid.velocity = transform.forward * currentMoveSpeed;


            Vector3 directionToTarget = (targetPos - transform.position);

            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, currentRotateSpeed * Time.deltaTime);
        }

        if (returningToDeposit)
            return;

        if (currentWaypointIndex >= myAgent.path.corners.Length)
            SetNewPath(NearestSnowballer());
    }
    #endregion


    Vector3 move;
    void GetMoveDirection()
    {
        if (IsGrounded())
        {
            move = Vector3.Cross(transform.forward, hit.normal);
        }
        else
        {
            // move = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
            ApplyGravity();
        }
    }

    void ApplyGravity()
    {
        if (!IsGrounded())
        {
            transform.position += Physics.gravity * Time.deltaTime / 1.5f;
        }
    }

    RaycastHit hit;
    public bool IsGrounded()
    {
        if (Physics.SphereCast(transform.position, 0.05f, -Vector3.up, out hit, 2.75f))
            return true;

        return false;
    }

    public float rotateSpeed;
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
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed);
        }
    }


    public Transform[] snowDeposits;
    bool returningToDeposit = false;

    public Transform currentDeposit;
    void ReturnToDeposit()
    {
        returningToDeposit = true;

        Transform tempDeposit = snowDeposits[0];
        Vector3 closestDeposit = snowDeposits[0].position;

        foreach (Transform snowDeposit in snowDeposits)
        {
            Vector3 tempPos = snowDeposit.position;

            if (Vector3.Distance(tempPos, transform.position) < Vector3.Distance(closestDeposit, transform.position))
            {
                closestDeposit = snowDeposit.position;
                tempDeposit = snowDeposit;
            }
        }

        currentDeposit = tempDeposit;
        SetNewPath(closestDeposit);

        ChangeColor();
    }

   void DepositSnow()
    {
        SnowDeposit SD = currentDeposit.transform.parent.GetComponent<SnowDeposit>();
        SD.AddSnow(size);


        size = 0;
        eatenText.text = size.ToString();
        CheckTrunk();
        returningToDeposit = false;
        ChangeColor();
    }

    void ChangeColor()
    {
        if(returningToDeposit)
            eatenText.color = Color.red;
        else
            eatenText.color = Color.white;
    }

    void CheckTrunk()
    {
        for (int i = 0; i < trunk.childCount; i++)
        {
            float currentSize = (float)((float)(i) / 5) * maxSize;
            trunk.GetChild(i).gameObject.SetActive(size > currentSize);
        }

        if (size >= maxSize)
        {
            ReturnToDeposit();
        }

    }

    void DrawAgentPath()
    {
        for (int i = 1; i < myAgent.path.corners.Length; i++)
        {
            Debug.DrawLine(myAgent.path.corners[i - 1], myAgent.path.corners[i], Color.red);
        }
    }

    bool playerCD = false;
    IEnumerator PlayerCooldown()
    {
        playerCD = true;

        yield return new WaitForSeconds(3f);

        playerCD = false;
    }

    bool thrownCD = false;
    IEnumerator ThrownCooldown()
    {
        thrownCD = true;

        yield return new WaitForSeconds(2f);

        thrownCD = false;
    }

    bool aiCD = false;
    IEnumerator AICooldown()
    {
        aiCD = true;

        yield return new WaitForSeconds(1f);

        aiCD = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Snow"))
        {
            size += other.GetComponent<Snow>().Size;

            other.transform.parent.SendMessage("UpdateSnowball", other.transform);

            eatenText.text = size.ToString();

            CheckTrunk();
        }

        if (other.tag.Equals("Snowballer"))
        {
            AISnowball aiScript = other.GetComponent<AISnowball>();
            SnowBallStats playerScript = other.GetComponent<SnowBallStats>();
            ThrownSnoball thrownScript = other.GetComponent<ThrownSnoball>();

            int amount = 0;

            if (aiScript != null)
            {
                amount = aiScript.GetSize;

                if (!aiCD)
                {
                    if (amount < 300)
                        aiScript.Swallowed();
                    else
                    {
                        amount = amount / 3;
                        aiScript.AddSnow(-amount);

                        eatenText.color = Color.red;
                        ReturnToDeposit();

                        StartCoroutine(AICooldown());
                    }
                }
            }

            if (playerScript != null)
            {
                amount = playerScript.GetSize;

                if (!playerCD) {
                    if (amount < 300)
                        playerScript.Swallowed();
                    else
                    {
                        amount = amount / 3;
                        playerScript.AddSnow(-amount);

                        eatenText.color = Color.red;
                        ReturnToDeposit();

                        StartCoroutine(PlayerCooldown());
                    }
                }
            }

            if (thrownScript != null)
            {
                amount = thrownScript.GetSize;

                if (!thrownCD)
                {
                    if (amount < 300)
                        thrownScript.Swallowed();
                    else
                    {
                        amount = amount / 3;
                        thrownScript.AddSnow(-amount);

                        eatenText.color = Color.red;
                        ReturnToDeposit();

                        StartCoroutine(ThrownCooldown());
                    }
                }
            }


            size += amount;
            eatenText.text = size.ToString();

            CheckTrunk();
        }

    }
}
