using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;


public class CarRLAgent : Agent
{
    [SerializeField] private Transform _goal;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private CarHandler carHandler; // CarHandler'ý kullan

    private float idleTimer = 0f;
    private Vector3 lastPosition;
    private const float idleThreshold = 2f;
    private const float movementTolerance = 0.01f;
    private bool isStuck = false;

    private int _currentEpisode = 0;
    private float _cumulativeReward = 0f;


    public override void Initialize()
    {
        _currentEpisode = 0;
        _cumulativeReward = 0f;
    }

    public override void OnEpisodeBegin()
    {
        if (carHandler.GetIsCrashed())
        {
            carHandler.SetIsCrashed(false);
        }

        if (isStuck)
        {
            Vector3 currentPos = transform.position;


            transform.position = new Vector3(0f, currentPos.y, currentPos.z);
            transform.localRotation = Quaternion.identity;

            isStuck = false;
        }
        

        carHandler.SetMaxSpeed(0.8f);
        //_currentEpisode++;
        _cumulativeReward = 0f;
        _renderer.material.color = Color.blue;

        idleTimer = 0f;
        lastPosition = transform.localPosition;

        SpawnObjects();
    }

    private void SpawnObjects()
    {

        // Hedefin konumunu rastgele ayarla
        float randomAngle = Random.Range(0f, 360f);
        Vector3 randomDirection = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward;
        float randomDistanceZ = Random.Range(30.0f, 60.0f);
        float randomDistanceX = Random.Range(0.1f, 0.1f);
        Vector3 goalPositionZ = transform.position + new Vector3(0,0, randomDistanceZ);
        Vector3 goalPositionX = transform.position + new Vector3(randomDistanceX, 0, 0);
        _goal.position = new Vector3(goalPositionX.x, 0.15f, goalPositionZ.z);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Hedef ve araba arasýndaki mesafeyi gözlemle
        float distanceToGoal = Vector3.Distance(transform.localPosition, _goal.localPosition);
        sensor.AddObservation(distanceToGoal);

        // Hedefe olan yönü gözlemle
        Vector3 directionToGoal = (_goal.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(directionToGoal.x);
        sensor.AddObservation(directionToGoal.z);

        // Arabanýn rotasyonunu gözlemle
        float carRotation_normalized = (transform.localRotation.eulerAngles.y / 360f) * 2f - 1f;
        sensor.AddObservation(carRotation_normalized);
    }

  
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (carHandler.GetIsCrashed())
        {
            EndEpisode();
        }

        // Eylemleri al (0: Sola dön, 1: Saða dön, 2: Düz git)
        var action = actions.DiscreteActions[0];

        // Ýleri hareket için hýzlandýrma, saða/sola hareket için yönlendirme ayarý
        float moveForward = 0f;
        float steerDirection = 0f;

        switch (action)
        {
            case 0: // Ýleri git
                moveForward = 0.1f; // Ýleri doðru hareket et
                steerDirection = 0.0f; // Düz gitmek için yönlendirme sýfýrla
                break;
            case 1: // Sola git
                steerDirection = -0.005f; // Sola doðru yönlendirme
                moveForward = 0.07f; // Ýleri doðru hareket et
                break;
            
            case 2: // Saða git
                steerDirection = 0.005f; // Saða doðru yönlendirme
                moveForward = 0.07f; // Ýleri doðru hareket et
                break;
        }

        // input.x: Yönlendirme (saða/sola), input.y: Hýzlanma (ileri)
        carHandler.SetInput(new Vector2(steerDirection, moveForward));


        // Eðer sola veya saða hareket etme aksiyonu seçildiyse Steer metodunu çaðýr.
        // Düz giderken de çaðýrýlabilir, çünkü Steer metodu input.x sýfýr olduðunda otomatik merkezleme yapar.
        carHandler.Steer();

        // Ödül ve ceza mekanizmasý
        float distanceToGoal = Vector3.Distance(transform.localPosition, _goal.localPosition);

        // Hedefe yaklaþtýkça ödül ver
        AddReward(0.01f / distanceToGoal);


        // Hedefi geçerse ceza ver
        if (distanceToGoal > 3f) // Hedefi geçme eþiði
        {
            AddReward(-0.01f);
        }



        _cumulativeReward = GetCumulativeReward();


        //Duvara takýlý kalýrsa yeniden baþlat
        float distanceMoved = Vector3.Distance(transform.localPosition, lastPosition);

        if (distanceMoved < movementTolerance)
        {
            idleTimer += Time.fixedDeltaTime;
            if (idleTimer >= idleThreshold)
            {
                AddReward(-200f);         // hareketsizlik cezasý (opsiyonel)
                isStuck = true;
                EndEpisode(); // yeniden baþlat

                carHandler.ResetCar();
            }
        }
        else
        {
            idleTimer = 0f;              // hareket ettiði için sýfýrla
            lastPosition = transform.localPosition;
        }
    }

    private void RotateTowardsGoal()
    {
        Vector3 directionToGoal = (_goal.localPosition - transform.localPosition).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToGoal);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _rotationSpeed * 0.01f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-10.0f);
            _renderer.material.color = Color.red;
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached();
        }

    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-1f * Time.fixedDeltaTime);
        }
    }

    private void GoalReached()
    {
        AddReward(100.0f);
        _cumulativeReward = GetCumulativeReward();
        _renderer.material.color = Color.green; // Hedefe ulaþtýðýnda rengi deðiþtir

        EndEpisode();
    }
}