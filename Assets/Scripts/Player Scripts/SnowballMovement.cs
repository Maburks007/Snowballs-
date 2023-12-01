using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowballMovement : MonoBehaviour
{
    public SnowBallStats SBS;

    Terrain[] terrains;

    public GameObject throwableSnowball;

    public Transform snowPlowersParent;
    public Transform snowballersParent;
    public Transform character;
    public Transform snowball;
    public Transform cameraPlacement;

    public Animator myAnim;

    public Rigidbody myRigid;


    public float cameraMaxRotation;

    public float maxPlayerSpeed;
    float currentPlayerSpeed;

    public float maxSnowballRotation;
    float currentSnowballRotation;

    public float maxRotateSpeed;
    float currentRotateSpeed;
    float lerpRotateSpeed;


    Quaternion playerRoation;
    Vector3 move;
    Vector3 moveDirection;


    bool playerReset = false;
    public bool SetPlayerReset
    {
        set { playerReset = value; }
    }


    private void Start()
    {
        currentPlayerSpeed = maxPlayerSpeed;
        currentRotateSpeed = maxRotateSpeed;
        currentSnowballRotation = maxSnowballRotation;

        terrains = Terrain.activeTerrains;
    }


    RaycastHit hit;
    // Update is called once per frame
    void Update()
    {
        CheckPositionOnTerrain();


        CalculateSlopeDirection();
        RotateCamera();
      //  ApplyGravity();
        RotatePlayer();


        if (!playerReset) 
        {
            CalculateRotation();
            MovePlayer();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(!throwSnowballCD)
            ThrowSnowball();
        }

        snowball.Rotate(Vector3.right * currentSnowballRotation * speedPenalty * Mathf.Abs(Input.GetAxisRaw("Vertical")));

        #region Animation Controlls
        //Checks if player is moving to change from Idle to Moving Animation
        if (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0)
            myAnim.SetBool("Moving", true);
        else
            myAnim.SetBool("Moving", false);
        #endregion
    }

    float sizeSlopePenalty = 0;
    public float decayValue;
    public void UpdateSnowBall(int newSize)
    {
        currentPlayerSpeed = maxPlayerSpeed *  ((Mathf.Exp((-1 / decayValue) * newSize)));
        currentRotateSpeed = maxRotateSpeed * Mathf.Exp((-1 / decayValue) * newSize * 2f);
        currentSnowballRotation = maxSnowballRotation  * Mathf.Exp((-1 / decayValue) * newSize);

       // sizeSlopePenalty = (Mathf.Exp((-1 / decayValue) * newSize) * 1.25f);
    }

    #region Player Abilities 


    #region Throw Snowball
    bool throwSnowballCD = false;
    void ThrowSnowball()
    {
        throwSnowballCD = true;
        StartCoroutine(ThrowSnowballCoolDown());

        int throwSize = SBS.GetSize / 3;

        SBS.AddSnow(-throwSize);

        GameObject thrownSnowball = Instantiate(throwableSnowball) as GameObject;
        thrownSnowball.GetComponent<ThrownSnoball>().AddSnow(throwSize);

        Vector3 snowballPos = transform.position + (transform.forward * ((snowball.lossyScale.x /2 + thrownSnowball.transform.lossyScale.x/2)* 1.15f));
        snowballPos = PositionOnTerrain(snowballPos);

        thrownSnowball.transform.position = snowballPos;
        thrownSnowball.transform.rotation = transform.rotation;


        ThrownBallUpdater();
    }

    IEnumerator ThrowSnowballCoolDown()
    {
        yield return new WaitForSeconds(6f);
        throwSnowballCD = false;
    }
    #endregion


    public void ThrownBallUpdater()
    {
        AISnowball[] snowballers = snowballersParent.GetComponentsInChildren<AISnowball>();
        SnowPlow[] snowplowers = snowPlowersParent.GetComponentsInChildren<SnowPlow>();

        foreach (AISnowball s in snowballers)
        {
            s.NewSnowballer();
        }

        foreach (SnowPlow s in snowplowers)
        {
            s.NewSnowballer();
        }
    }

    #endregion

    float speedPenalty = 1f;
    void MovePlayer()
    {
        myRigid.velocity = move * currentPlayerSpeed * speedPenalty;

        transform.Rotate(playerRoation.eulerAngles);
    }


    void ApplyGravity()
    {
        if (!IsGrounded())
        {
            transform.position += Physics.gravity * Time.deltaTime / 1.5f;
        }
    }


    Terrain GetCurrentTerrain()
    {

        if (terrains == null)
        {
            Debug.LogError("Terrain not assigned!");
        }

        //Checks which object the terrain is in
        foreach (Terrain t in terrains)
        {
            TerrainData tData = t.terrainData;

            if (transform.position.x >= t.transform.position.x &&
               transform.position.x <= t.transform.position.x + tData.size.x &&
               transform.position.z >= t.transform.position.z &&
               transform.position.z <= t.transform.position.z + tData.size.z)
            {
                return t;
            }
        }

        return null;
    }

    Vector3 PositionOnTerrain(Vector3 currentPos)
    {
        Terrain currentTerrain = GetCurrentTerrain();
        if (currentTerrain != null)
        {

            TerrainData terrainData = currentTerrain.terrainData;

            float terrainHeight = terrainData.GetInterpolatedHeight(
                (currentPos.x - currentTerrain.transform.position.x) / terrainData.size.x,
                (currentPos.z - currentTerrain.transform.position.z) / terrainData.size.z
            );

            return new Vector3(currentPos.x, terrainHeight, currentPos.z);
        }

        return Vector3.zero;
    }

    void CheckPositionOnTerrain()
    {
        if (transform.position.y < PositionOnTerrain(transform.position).y - 1f)
        {
            transform.position = PositionOnTerrain(transform.position);
        }
    }
    public float angleRotateSpeed;
    void RotatePlayer()
    {
        Terrain currentTerrain = GetCurrentTerrain();

     
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
            character.rotation = Quaternion.Lerp(character.rotation, targetRotation, angleRotateSpeed * Time.deltaTime);
        }

    }


     float cameraRotation = 90;
     float localCameraRotation = 90;
     bool isRotating = false;
    void RotateCamera()
    {
        if (Mathf.Abs(localCameraRotation - cameraPlacement.localEulerAngles.y) < cameraMaxRotation)
        {
            if (Input.GetAxisRaw("Horizontal") != 0 && isRotating == false)
            {
                isRotating = true;
                cameraRotation = cameraPlacement.eulerAngles.y;
                localCameraRotation = cameraPlacement.localEulerAngles.y;
            }


            cameraPlacement.eulerAngles = new Vector3(0, cameraRotation, 0);
        }

        if (Input.GetAxisRaw("Horizontal") == 0)
        {
            isRotating = false;
            cameraPlacement.localEulerAngles = new Vector3(0, Mathf.Lerp(cameraPlacement.localEulerAngles.y, 90, 0.05f), 0);
            cameraRotation = cameraPlacement.eulerAngles.y;         
        }
    }

    public float lerpDuration;
    float timeElapsed;
    void CalculateRotation()
    {
        playerRoation = Quaternion.Euler(transform.up * Input.GetAxisRaw("Horizontal") * currentRotateSpeed);

        //Rotates the player based on how long they are rotating
        if (Input.GetAxisRaw("Horizontal") != 0)
        {
            if (timeElapsed < lerpDuration) {
                lerpRotateSpeed = Mathf.Lerp(0, currentRotateSpeed, (timeElapsed / lerpDuration));
                timeElapsed += Time.deltaTime;
            }
            else
            {
                lerpRotateSpeed = currentRotateSpeed;
            }
        }
        else
        {
            lerpRotateSpeed = 0;
            timeElapsed = 0;
        }
    }

    void CalculateSlopeDirection()
    {
        if (IsGrounded())
        {
            move = Vector3.Cross(transform.right * Input.GetAxis("Vertical"), hit.normal);
        }
        else
        {
            move = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
            ApplyGravity();
        }

        speedPenalty = Mathf.Clamp(1 - (move.y + (move.y * sizeSlopePenalty)), 0, 5);

        moveDirection = transform.position + move;
    }


    public bool IsGrounded()
    {
        if (Physics.SphereCast(transform.position, 0.05f, -Vector3.up, out hit, (transform.localScale.y) * 0.75f))
            return true;

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(moveDirection, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.Cross(transform.right * Input.GetAxis("Vertical"), hit.normal));

        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + hit.normal);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(hit.point, 0.25f);
    }
}
