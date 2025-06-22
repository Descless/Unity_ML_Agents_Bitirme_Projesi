using System.Collections;
using Unity.MLAgents;
using UnityEngine;

public class AIHandler : MonoBehaviour
{

    [SerializeField]
    MeshCollider meshCollider;

    RaycastHit[] raycastHits = new RaycastHit[1];

    int drivingInLane = 0;

    WaitForSeconds wait = new WaitForSeconds(0.2f);

    [SerializeField]
    Rigidbody rb;

    [SerializeField]
    Transform gameModel;


    float maxSteerVelocity = 2;
    float maxForwardVelocity = 3f;
    float accelerationMultiplier = 1f;
    float breakMultiplier = 15;
    float steeringMultiplier = 5;
    private Vector2 input = Vector2.zero;


    float carStartPositionZ;
    float distanceTravelled = 0;
    public float DistanceTravelled => distanceTravelled;


    bool isPlayer = false;
    void Start()
    {
        if (GetComponent<Agent>() != null)  // bu RL ajan ise
            return;


        SetMaxSpeed(Random.Range(0.5f, 1f));

        SetInput(new Vector2(0f, 1f));

        carStartPositionZ = transform.position.z;

    }


    private void FixedUpdate()
    {
        if (input.y > 0)
            Accelerate();
        else
            rb.linearDamping = 0.2f;

        if (input.y < 0)
            Brake();

        Steer();

        if (rb.linearVelocity.z <= 0)
            rb.linearVelocity = Vector3.zero;
    }

    public void Accelerate()
    {
        rb.linearDamping = 0;

        if (rb.linearVelocity.z >= maxForwardVelocity)
            return;

        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    void Brake()
    {
        if (rb.linearVelocity.z <= 0)
            return;

        rb.AddForce(rb.transform.forward * breakMultiplier * input.y);
    }

    public void Steer()
    {


        if (Mathf.Abs(input.x) > 0)
        {
            //Move car sideway
            //float speedBaseSteerLimit = rb.linearVelocity.z / 5.0f;

            float speedBaseSteerLimit = 1.0f;

            speedBaseSteerLimit = Mathf.Clamp01(speedBaseSteerLimit);

            rb.AddForce(rb.transform.right * steeringMultiplier * input.x * speedBaseSteerLimit);

            float normalizedX = rb.linearVelocity.x / maxSteerVelocity;

            //no bigger than 1 in magnitued
            normalizedX = Mathf.Clamp(normalizedX, -1.0f, 1.0f);


            //Speed Limit
            rb.linearVelocity = new Vector3(normalizedX * maxSteerVelocity, 0, rb.linearVelocity.z);
        }

        else
        {
            //auto center
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(0, 0, rb.linearVelocity.z), Time.fixedDeltaTime * 3);

        }
    }

    public void SetInput(Vector2 inputVector)
    {
        inputVector.Normalize();
        input = inputVector;
    }



    public void SetMaxSpeed(float newMaxSpeed)
    {
        maxForwardVelocity = newMaxSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
         if (collision.transform.root.CompareTag("Untagged"))
             return;

         if (collision.transform.root.CompareTag("Car AI"))
             return;
     
    }


private void Awake()
    {
        if (CompareTag("Player"))
        {
            Destroy(this);
            return;
        }

        if (GetComponent<Agent>() != null)
        {
            enabled = false;  // Destroy etme, disable et
            return;
        }
    }


    void Update()
    {
        gameModel.transform.rotation = Quaternion.Euler(0, rb.linearVelocity.x * 5, 0);

        distanceTravelled = transform.position.z - carStartPositionZ;

        float accelerationInput = 1.0f;

        float steerInput = 0.0f;

        float desiredPositionX = Utils.CarLanes[drivingInLane];

        float difference = desiredPositionX - transform.position.x;

        if (Mathf.Abs(difference) > 0.05f)
        {
            steerInput = 1.0f * difference;
        }

        steerInput = Mathf.Clamp(steerInput, -1.0f, 1.0f);

        SetInput(new Vector2(steerInput, accelerationInput));
    }


    private void OnEnable()
    {
        drivingInLane = Random.Range(0, Utils.CarLanes.Length);
    }
}
