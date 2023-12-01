using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ThrownSnoball : MonoBehaviour
{
    Rigidbody myRigid;

    public TextMeshProUGUI sizeText;

    public Transform sizeCanvas;
    public Transform snowball;

    [SerializeField] int snowballSize;
    public int GetSize
    {
        get { return snowballSize; }
    }

    public float decayValue;


    public float maxRotateSpeed;
    float currentRotateSpeed;

    public float maxMoveSpeed;
    float currentMoveSpeed;

    private void Start()
    {
        myRigid = GetComponent<Rigidbody>();


        StartCoroutine(ThrowTimer());
    }

    bool hasDied = false;
    private void Update()
    {
        CheckPositionOnTerrain();

        sizeCanvas.LookAt(Camera.main.transform);

        snowball.Rotate(Vector3.right * myRigid.velocity.magnitude * currentRotateSpeed * 0.25f);


        CalculateSlopeDirection();

        if (!hasDied)
            MoveSnowball();
    }

    void MoveSnowball()
    {
        myRigid.velocity = move * currentMoveSpeed * 1.35f * speedPenalty;
    }

    IEnumerator ThrowTimer()
    {
        yield return new WaitForSeconds(1.3f);

        myRigid.velocity = Vector3.zero;
        hasDied = true;
    }

    public void AddSnow(int snowAmount)
    {
        snowballSize += snowAmount;

        transform.localScale = Vector3.one * (1 + (snowballSize * 0.00625f));

        currentMoveSpeed = maxMoveSpeed * ((Mathf.Exp((-1 / decayValue) * snowballSize)));
        currentRotateSpeed = maxRotateSpeed * Mathf.Exp((-1 / decayValue) * snowballSize * 2f);

        sizeCanvas.localPosition = new Vector3(sizeCanvas.localPosition.x, 0.8f + (snowballSize * 0.0075f), sizeCanvas.localPosition.z);
        sizeText.fontSize = 0.5f * (1 + (snowballSize * 0.002f));
        sizeText.text = snowballSize.ToString();
    }

    Vector3 moveDirection;
    Vector3 move;
    float speedPenalty;
    void CalculateSlopeDirection()
    {
        if (IsGrounded())
        {
            move = Vector3.Cross(transform.right, hit.normal);
        }
        else
        {
            // move = transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical");
            ApplyGravity();
        }

        speedPenalty = Mathf.Clamp(1 - (move.y + (move.y)), 0, 5);

        moveDirection = transform.position + move;
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
        if (Physics.SphereCast(transform.position, 0.05f, -Vector3.up, out hit, (transform.localScale.y) * 0.75f))
            return true;

        return false;
    }

    Terrain GetCurrentTerrain()
    {
        Terrain[] terrains = Terrain.activeTerrains;

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

    public void Swallowed()
    {
        Destroy(transform.gameObject);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Snow"))
        {
            AddSnow(other.GetComponent<Snow>().Size);

            other.transform.parent.SendMessage("UpdateSnowball", other.transform);
        }
        else if (other.tag.Equals("Snowballer"))
        {
            SnowBallStats player = other.GetComponent<SnowBallStats>();
            if (player != null)
                return;

            int targetSize = 0;

            AISnowball aiScript = other.GetComponent<AISnowball>();
            if (aiScript != null)
                targetSize = aiScript.GetSize;

            ThrownSnoball thrownBallScript = other.GetComponent<ThrownSnoball>();
            if (thrownBallScript != null)
                targetSize = thrownBallScript.GetSize;

            if(targetSize + (targetSize * 0.3f) < snowballSize)
            {
                AddSnow(targetSize);

                other.SendMessage("Swallowed");
            }

        }

    }
}
