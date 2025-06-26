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

    private float idleTimer = 0f; //saya�
    private Vector3 lastPosition; //Araban�n son pozisyonu
    private const float idleThreshold = 2f;
    private const float movementTolerance = 0.01f;
    private bool isStuck = false;
    private bool crashedIntoCar = false;
    private bool isGoalReached = false;

    public int CurrentEpisode = 0;
    public float CumulativeReward = 0f;

    private float previousDistance;



    public override void Initialize()
    {
        CurrentEpisode = 0;
        CumulativeReward = 0f;
    }

    public override void OnEpisodeBegin()
    {
        //�arpt�ysa s�f�rla
        if (carHandler.GetIsCrashed())
        {
            carHandler.SetIsCrashed(false);
        }

        //Tak�l� kald�ysa
        if (isStuck)
        {
            Vector3 currentPos = transform.position;


            transform.position = new Vector3(0f, currentPos.y, currentPos.z); //x de�erini s�f�rla yolun ortas�na g�t�r
            transform.localRotation = Quaternion.identity;

            isStuck = false; //de�i�keni s�f�rla
        }
        
        //Arac�n maksimum h�z�n� ayarla
        carHandler.SetMaxSpeed(1.4f);
        CurrentEpisode++;
        CumulativeReward = 0f; //Toplam �d�l� s�f�rla
        _renderer.material.color = Color.blue;

        previousDistance = Vector3.Distance(transform.localPosition, _goal.localPosition); //Hedef ve araba aras�ndaki mesafeyi al

        idleTimer = 0f; //S�reyi s�f�rla
        lastPosition = transform.localPosition; //Araban�n son pozisyonunu al

        SpawnObjects(); //Hedef �ret
    }

    private void SpawnObjects()
    {

        // Hedefin konumunu rastgele ayarla
        float randomAngle = Random.Range(0f, 360f);
        Vector3 randomDirection = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward; //Rastgele a�� ver
        float randomDistanceZ = Random.Range(3.0f, 7.0f); //Rastgele z de�eri
        float randomDistanceX = Random.Range(-0.3f, 0.3f); //Rastgele x de�eri
        Vector3 goalPosition = new Vector3(randomDistanceX, transform.position.y, transform.position.z + randomDistanceZ); //Arac�n z de�erinin biraz ilerisine koy, rastgele x de�erine koy, ara�la ayn� y de�erini al (Ara�la ayn� y�kseklikte, arac�n biraz ilerisinde, yolun rastgele bir yerinde)
        _goal.position = goalPosition; //pozisyonunu ayarla
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

        //Engelleri g�zlemle
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        float nearestCarDist = 10f;

        //Ba�ka araba  var m� tespit et
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Car AI"))
            {

                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < nearestCarDist) nearestCarDist = d;
            }

            if (hit.CompareTag("Car AI") && hit.gameObject != gameObject)
            {

                Vector3 direction = (hit.transform.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, hit.transform.position);

                sensor.AddObservation(direction.x);
                sensor.AddObservation(direction.z);
                sensor.AddObservation(distance / 10f); // normalize edilmi� mesafe


            }
        }


        sensor.AddObservation(nearestCarDist / 10f); // normalize edilmi� uzakl�k

    }


    public override void OnActionReceived(ActionBuffers actions) {

        if (crashedIntoCar)
        {
            if (StepCount > 10)
                AddReward(-2.0f); // Ceza

            EndEpisode();     // Birka� frame sonra bitir
            crashedIntoCar = false;
            return;
        }

        if (isGoalReached)
        {
            //�d�l ver ve b�l�m� bitir
            AddReward(2.0f);
            CumulativeReward = GetCumulativeReward();
            _renderer.material.color = Color.green;

            EndEpisode();
            isGoalReached = false;
            return;
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
                steerDirection = 0.0f; 
                break;
            case 1: // Sola git
                steerDirection = -0.005f; // Sola do�ru y�nlendirme
                moveForward = 0.04f; 
                break;

            case 2: // Sa�a git
                steerDirection = 0.005f; // Sa�a do�ru y�nlendirme
                moveForward = 0.04f; 
                break;
        }

        // input.x: Y�nlendirme (sa�a/sola), input.y: H�zlanma (ileri)
        carHandler.SetInput(new Vector2(steerDirection, moveForward));


        // E�er sola veya sa�a hareket etme aksiyonu se�ildiyse Steer metodunu �a��r.
        // D�z giderken de �a��r�labilir, ��nk� Steer metodu input.x s�f�r oldu�unda otomatik merkezleme yapar.
        carHandler.Steer();

        float distanceToGoal = Vector3.Distance(transform.localPosition, _goal.localPosition);

        //�d�l Sistemi
        // Hedefi ge�erse ceza ver
        if (distanceToGoal > 3f)
        {
            AddReward(-0.005f); // daha az
        }

        float delta = previousDistance - distanceToGoal;
        if (delta > 0)
            AddReward(delta * 0.4f);  // sadece yakla��rsa �d�l ver
        else
            AddReward(delta * 0.1f);  // uzakla��rsa daha k���k ceza 
        previousDistance = distanceToGoal;

        //�d�lleri topla
        CumulativeReward = GetCumulativeReward();


        //Duvara tak�l� kal�rsa yeniden ba�lat
        float distanceMoved = Vector3.Distance(transform.localPosition, lastPosition);

        if (distanceMoved < movementTolerance)
        {
            idleTimer += Time.fixedDeltaTime;
            if (idleTimer >= idleThreshold)
            {
                AddReward(-1f);         // hareketsizlik cezas� (opsiyonel)
                isStuck = true;
                EndEpisode(); // B�l�m� bitir

                //Arac� s�f�rla
                carHandler.ResetCar();
            }
        }
        else
        {
            idleTimer = 0f;              // hareket etti�i i�in s�f�rla
            lastPosition = transform.localPosition;
        }

        // Di�er ara�lara olan en yak�n mesafeyi kontrol et
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        float nearestCarDist = 10f;

        // Ba�ka ara� var m� tespit et
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Car AI") && hit.gameObject != gameObject)
            {
                float d = Vector3.Distance(transform.position, hit.transform.position);
                if (d < nearestCarDist)
                    nearestCarDist = d;
            }
        }

        Heuristic(actions);
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
      
    }

    private void OnCollisionEnter(Collision collision)
    {
        //Ba�ka araca �arparsa
        if (collision.gameObject.CompareTag("Car AI"))
        {
            crashedIntoCar = true;

            //AddReward(-1.0f); // �arpt�ysa ceza
            //EndEpisode();     // veya hemen bitir

            collision.gameObject.SetActive(false); // �arpan arac� pasifle�tir
        }

        // Duvara �arparsa
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f); //Ceza ver
            _renderer.material.color = Color.red; //K�rm�z�ya d�nd�r
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Hedefe ula��rsa
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached(); //�d�l ver ve b�l�m� bitir
        }

    }

    private void OnCollisionStay(Collision collision)
    {
        //Duvara tak�l� tak�l�rsa
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.02f * Time.fixedDeltaTime); //Her saniye ceza ver
        }

    }

    private void GoalReached()
    {
        isGoalReached = true; //Hedefe ula�t�
        
    }
}