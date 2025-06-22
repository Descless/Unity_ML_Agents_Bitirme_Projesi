using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;


public class CarRLAgent : Agent
{
    [SerializeField] private Transform _goal;
    [SerializeField] private float _rotationSpeed = 180f;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private CarHandler carHandler; // CarHandler'� kullan

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
        // Hedef ve araba aras�ndaki mesafeyi g�zlemle
        float distanceToGoal = Vector3.Distance(transform.localPosition, _goal.localPosition);
        sensor.AddObservation(distanceToGoal);

        // Hedefe olan y�n� g�zlemle
        Vector3 directionToGoal = (_goal.localPosition - transform.localPosition).normalized;
        sensor.AddObservation(directionToGoal.x);
        sensor.AddObservation(directionToGoal.z);

        // Araban�n rotasyonunu g�zlemle
        float carRotation_normalized = (transform.localRotation.eulerAngles.y / 360f) * 2f - 1f;
        sensor.AddObservation(carRotation_normalized);
    }

  
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (carHandler.GetIsCrashed())
        {
            EndEpisode();
        }

        // Eylemleri al (0: Sola d�n, 1: Sa�a d�n, 2: D�z git)
        var action = actions.DiscreteActions[0];

        // �leri hareket i�in h�zland�rma, sa�a/sola hareket i�in y�nlendirme ayar�
        float moveForward = 0f;
        float steerDirection = 0f;

        switch (action)
        {
            case 0: // �leri git
                moveForward = 0.1f; // �leri do�ru hareket et
                steerDirection = 0.0f; // D�z gitmek i�in y�nlendirme s�f�rla
                break;
            case 1: // Sola git
                steerDirection = -0.005f; // Sola do�ru y�nlendirme
                moveForward = 0.07f; // �leri do�ru hareket et
                break;
            
            case 2: // Sa�a git
                steerDirection = 0.005f; // Sa�a do�ru y�nlendirme
                moveForward = 0.07f; // �leri do�ru hareket et
                break;
        }

        // input.x: Y�nlendirme (sa�a/sola), input.y: H�zlanma (ileri)
        carHandler.SetInput(new Vector2(steerDirection, moveForward));


        // E�er sola veya sa�a hareket etme aksiyonu se�ildiyse Steer metodunu �a��r.
        // D�z giderken de �a��r�labilir, ��nk� Steer metodu input.x s�f�r oldu�unda otomatik merkezleme yapar.
        carHandler.Steer();

        // �d�l ve ceza mekanizmas�
        float distanceToGoal = Vector3.Distance(transform.localPosition, _goal.localPosition);

        // Hedefe yakla�t�k�a �d�l ver
        AddReward(0.01f / distanceToGoal);


        // Hedefi ge�erse ceza ver
        if (distanceToGoal > 3f) // Hedefi ge�me e�i�i
        {
            AddReward(-0.01f);
        }



        _cumulativeReward = GetCumulativeReward();


        //Duvara tak�l� kal�rsa yeniden ba�lat
        float distanceMoved = Vector3.Distance(transform.localPosition, lastPosition);

        if (distanceMoved < movementTolerance)
        {
            idleTimer += Time.fixedDeltaTime;
            if (idleTimer >= idleThreshold)
            {
                AddReward(-200f);         // hareketsizlik cezas� (opsiyonel)
                isStuck = true;
                EndEpisode(); // yeniden ba�lat

                carHandler.ResetCar();
            }
        }
        else
        {
            idleTimer = 0f;              // hareket etti�i i�in s�f�rla
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
        _renderer.material.color = Color.green; // Hedefe ula�t���nda rengi de�i�tir

        EndEpisode();
    }
}