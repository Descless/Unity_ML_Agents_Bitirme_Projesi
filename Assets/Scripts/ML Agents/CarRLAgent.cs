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

    private float idleTimer = 0f; //sayaç
    private Vector3 lastPosition; //Arabanýn son pozisyonu
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
        //Çarptýysa sýfýrla
        if (carHandler.GetIsCrashed())
        {
            carHandler.SetIsCrashed(false);
        }

        //Takýlý kaldýysa
        if (isStuck)
        {
            Vector3 currentPos = transform.position;


            transform.position = new Vector3(0f, currentPos.y, currentPos.z); //x deðerini sýfýrla yolun ortasýna götür
            transform.localRotation = Quaternion.identity;

            isStuck = false; //deðiþkeni sýfýrla
        }
        
        //Aracýn maksimum hýzýný ayarla
        carHandler.SetMaxSpeed(1.4f);
        CurrentEpisode++;
        CumulativeReward = 0f; //Toplam ödülü sýfýrla
        _renderer.material.color = Color.blue;

        previousDistance = Vector3.Distance(transform.localPosition, _goal.localPosition); //Hedef ve araba arasýndaki mesafeyi al

        idleTimer = 0f; //Süreyi sýfýrla
        lastPosition = transform.localPosition; //Arabanýn son pozisyonunu al

        SpawnObjects(); //Hedef üret
    }

    private void SpawnObjects()
    {

        // Hedefin konumunu rastgele ayarla
        float randomAngle = Random.Range(0f, 360f);
        Vector3 randomDirection = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward; //Rastgele açý ver
        float randomDistanceZ = Random.Range(3.0f, 7.0f); //Rastgele z deðeri
        float randomDistanceX = Random.Range(-0.3f, 0.3f); //Rastgele x deðeri
        Vector3 goalPosition = new Vector3(randomDistanceX, transform.position.y, transform.position.z + randomDistanceZ); //Aracýn z deðerinin biraz ilerisine koy, rastgele x deðerine koy, araçla ayný y deðerini al (Araçla ayný yükseklikte, aracýn biraz ilerisinde, yolun rastgele bir yerinde)
        _goal.position = goalPosition; //pozisyonunu ayarla
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

        //Engelleri gözlemle
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        float nearestCarDist = 10f;

        //Baþka araba  var mý tespit et
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
                sensor.AddObservation(distance / 10f); // normalize edilmiþ mesafe


            }
        }


        sensor.AddObservation(nearestCarDist / 10f); // normalize edilmiþ uzaklýk

    }


    public override void OnActionReceived(ActionBuffers actions) {

        if (crashedIntoCar)
        {
            if (StepCount > 10)
                AddReward(-2.0f); // Ceza

            EndEpisode();     // Birkaç frame sonra bitir
            crashedIntoCar = false;
            return;
        }

        if (isGoalReached)
        {
            //Ödül ver ve bölümü bitir
            AddReward(2.0f);
            CumulativeReward = GetCumulativeReward();
            _renderer.material.color = Color.green;

            EndEpisode();
            isGoalReached = false;
            return;
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
                steerDirection = 0.0f; 
                break;
            case 1: // Sola git
                steerDirection = -0.005f; // Sola doðru yönlendirme
                moveForward = 0.04f; 
                break;

            case 2: // Saða git
                steerDirection = 0.005f; // Saða doðru yönlendirme
                moveForward = 0.04f; 
                break;
        }

        // input.x: Yönlendirme (saða/sola), input.y: Hýzlanma (ileri)
        carHandler.SetInput(new Vector2(steerDirection, moveForward));


        // Eðer sola veya saða hareket etme aksiyonu seçildiyse Steer metodunu çaðýr.
        // Düz giderken de çaðýrýlabilir, çünkü Steer metodu input.x sýfýr olduðunda otomatik merkezleme yapar.
        carHandler.Steer();

        float distanceToGoal = Vector3.Distance(transform.localPosition, _goal.localPosition);

        //Ödül Sistemi
        // Hedefi geçerse ceza ver
        if (distanceToGoal > 3f)
        {
            AddReward(-0.005f); // daha az
        }

        float delta = previousDistance - distanceToGoal;
        if (delta > 0)
            AddReward(delta * 0.4f);  // sadece yaklaþýrsa ödül ver
        else
            AddReward(delta * 0.1f);  // uzaklaþýrsa daha küçük ceza 
        previousDistance = distanceToGoal;

        //Ödülleri topla
        CumulativeReward = GetCumulativeReward();


        //Duvara takýlý kalýrsa yeniden baþlat
        float distanceMoved = Vector3.Distance(transform.localPosition, lastPosition);

        if (distanceMoved < movementTolerance)
        {
            idleTimer += Time.fixedDeltaTime;
            if (idleTimer >= idleThreshold)
            {
                AddReward(-1f);         // hareketsizlik cezasý (opsiyonel)
                isStuck = true;
                EndEpisode(); // Bölümü bitir

                //Aracý sýfýrla
                carHandler.ResetCar();
            }
        }
        else
        {
            idleTimer = 0f;              // hareket ettiði için sýfýrla
            lastPosition = transform.localPosition;
        }

        // Diðer araçlara olan en yakýn mesafeyi kontrol et
        Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
        float nearestCarDist = 10f;

        // Baþka araç var mý tespit et
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
        //Baþka araca çarparsa
        if (collision.gameObject.CompareTag("Car AI"))
        {
            crashedIntoCar = true;

            //AddReward(-1.0f); // çarptýysa ceza
            //EndEpisode();     // veya hemen bitir

            collision.gameObject.SetActive(false); // Çarpan aracý pasifleþtir
        }

        // Duvara çarparsa
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.5f); //Ceza ver
            _renderer.material.color = Color.red; //Kýrmýzýya döndür
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //Hedefe ulaþýrsa
        if (other.gameObject.CompareTag("Goal"))
        {
            GoalReached(); //Ödül ver ve bölümü bitir
        }

    }

    private void OnCollisionStay(Collision collision)
    {
        //Duvara takýlý takýlýrsa
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.02f * Time.fixedDeltaTime); //Her saniye ceza ver
        }

    }

    private void GoalReached()
    {
        isGoalReached = true; //Hedefe ulaþtý
        
    }
}