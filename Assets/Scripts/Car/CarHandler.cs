using System;
using UnityEngine;
using UnityEngine.SceneManagement;
public class CarHandler : MonoBehaviour
{

    [SerializeField]
    Rigidbody rb;

    [SerializeField]
    Transform gameModel;

    [Header("SFX")]
    [SerializeField]
    AudioSource carEngineAS;

    [SerializeField]
    AnimationCurve carPitchAnimationCurve;

    [SerializeField]
    AudioSource carSkidAS;

    [SerializeField]
    CrashHandler crashHandler;

    float maxSteerVelocity = 2;
    float maxForwardVelocity = 20;
    float accelerationMultiplier = 3;
    float breakMultiplier = 15;
    float steeringMultiplier = 5;
    private Vector2 input = Vector2.zero;


    float carStartPositionZ;
    float distanceTravelled = 0;
    public float DistanceTravelled => distanceTravelled;


    bool isPlayer = false;
    bool isCrashed = false;
    void Start()
    {
        isPlayer = CompareTag("Player"); 
        
        if (isPlayer)
        {
            carEngineAS.Play();
        }

        carStartPositionZ = transform.position.z;

    }

  
    void Update()
    {
        gameModel.transform.rotation = Quaternion.Euler(0, rb.linearVelocity.x * 5, 0);

        UpdateCarAudio();
       // if is exploded FadeOutCarAudio();


        distanceTravelled = transform.position.z - carStartPositionZ;
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.z >= maxForwardVelocity)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, maxForwardVelocity);


        if (isCrashed)
        {
            rb.linearDamping = rb.linearVelocity.z * 0.1f;
            rb.linearDamping = Mathf.Clamp(rb.linearDamping, 1.5f, 10);

            rb.MovePosition(Vector3.Lerp(transform.position, new Vector3(0,0,transform.position.z), Time.deltaTime * 0.5f));

            return;
        }

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


        if (Mathf.Abs(input.x)>0)
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

    void UpdateCarAudio()
    {
        if (!isPlayer)
            return;

        float carMaxSpeedPercentage = rb.linearVelocity.z / maxForwardVelocity;

        carEngineAS.pitch = carPitchAnimationCurve.Evaluate(carMaxSpeedPercentage);

        if (input.y < 0 && carMaxSpeedPercentage > 0.2f)
        {
            if (!carSkidAS.isPlaying)
                carSkidAS.Play();

            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 1.0f, Time.deltaTime * 10);

        }
        else
        {
            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 0 , Time.deltaTime * 30);
        }
    }

    void FadeOutCarAudio()
    {
        if (!isPlayer) return;

        carEngineAS.volume = Mathf.Lerp(carEngineAS.volume, 0, Time.deltaTime * 10);
        carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 0, Time.deltaTime * 10);

    }

    public void SetMaxSpeed(float newMaxSpeed)
    {
        maxForwardVelocity = newMaxSpeed;
    }

    public bool GetIsCrashed()
    {
        return isCrashed;
    }
    public void SetIsCrashed(bool crashed)
    {
        isCrashed = crashed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.root.CompareTag("Car AI"))
        {
            Vector3 velocity = rb.linearVelocity;

            crashHandler.Crash(velocity * 45);

            isCrashed = true;
        }
    }
    public void ResetCar()
    {
        isCrashed = false;
        input = Vector2.zero;
        
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
