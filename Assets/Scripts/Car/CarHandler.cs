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

    float maxSteerVelocity = 150.0f;
    float maxForwardVelocity = 150.0f;
    float accelerationMultiplier = 100;
    float breakMultiplier = 15;
    float steeringMultiplier = 5;
    private Vector2 input = Vector2.zero;


    float carStartPositionZ;
    float distanceTravelled = 0;
    public float DistanceTravelled => distanceTravelled;

    bool isCrashed = false;
    void Start()
    {

        carEngineAS.Play(); // Araba motor sesini baþlat

        carStartPositionZ = transform.position.z; // Arabanýn baþlangýç z pozisyonunu al

    }

  
    void Update()
    {
        SetInput(new Vector2(0,30.0f));
        gameModel.transform.rotation = Quaternion.Euler(0, rb.linearVelocity.x * 5, 0); // Araba modelini döndür

        UpdateCarAudio(); // Araba sesini güncelle

        distanceTravelled = transform.position.z - carStartPositionZ; // Arabanýn ne kadar mesafe katettiðini hesapla
    }

    private void FixedUpdate()
    {
        //Maksimum hýza kadar hýzlanmayý saðlýyor
        if (rb.linearVelocity.z >= maxForwardVelocity)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, maxForwardVelocity);

        //Araba çarptýysa
        if (isCrashed)
        {
            rb.linearDamping = rb.linearVelocity.z * 0.1f;
            rb.linearDamping = Mathf.Clamp(rb.linearDamping, 1.5f, 10);

            rb.MovePosition(Vector3.Lerp(transform.position, new Vector3(0,0,transform.position.z), Time.deltaTime * 0.5f)); //Arabayý savur

            return;
        }
        //Arabaya güç uygulandýðý zaman ileri hareket ettir
        if (input.y > 0)
            Accelerate();
        else
            rb.linearDamping = 0.2f;
        //Arabaya güç uygulanmadýysa yavaþlat
        if (input.y < 0)
            Brake();
        //Arabayý saða veya sola hareket ettir
        Steer();

        //Geriye doðru hareket etmesini engelle
        if (rb.linearVelocity.z <= 0)
            rb.linearVelocity = Vector3.zero;
    }

    public void Accelerate()
    {
        rb.linearDamping = 0;

        //Maksimum hýza ulaþtýysa hýzlanmayý engelle
        if (rb.linearVelocity.z >= maxForwardVelocity)
            return;

        //Hýzlanma kuvveti uygula
        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    void Brake()
    {
        //Eðer araba geri hareket ediyorsa, geri hareket etmesini engelle
        if (rb.linearVelocity.z <= 0)
            return;

        //Eðer araba ileri hareket ediyorsa, fren kuvveti uygula
        rb.AddForce(rb.transform.forward * breakMultiplier * input.y);
    }

    public void Steer()
    {

       
        if (Mathf.Abs(input.x)>0)
        {
            
            float speedBaseSteerLimit = 1.0f;

            //Dönme hýzý için maksimum deðeri ayarla
            speedBaseSteerLimit = Mathf.Clamp01(speedBaseSteerLimit);

            // Eðer araba saða veya sola hareket ediyorsa, saða veya sola kuvvet uygula
            rb.AddForce(rb.transform.right * steeringMultiplier * input.x * speedBaseSteerLimit);

            // Maksimum dönme hýzýna göre normalize et
            float normalizedX = rb.linearVelocity.x / maxSteerVelocity;

            // Normalize edilmiþ deðeri -1 ile 1 arasýnda sýnýrla
            normalizedX = Mathf.Clamp(normalizedX, -1.0f, 1.0f);


            // Arabanýn x eksenindeki hýzýný, maksimum direksiyon hýzý ile çarp ve yeni bir hýz vektörü oluþtur
            rb.linearVelocity = new Vector3(normalizedX * maxSteerVelocity, 0, rb.linearVelocity.z);
        }

        else
        {
            //auto center
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(0, 0, rb.linearVelocity.z), Time.fixedDeltaTime * 3);
        
        }
    }

    //Kuvvet deðerini ayarla
    public void SetInput(Vector2 inputVector)
    {
       
        inputVector.Normalize(); // Normalize et ki deðerler -1 ile 1 arasýnda olsun
        input = inputVector;
    }

    //Araba sesini güncelle
    void UpdateCarAudio()
    {
        // Arabanýn maksimum hýzýna göre normalize et
        float carMaxSpeedPercentage = rb.linearVelocity.z / maxForwardVelocity;

        // Arabanýn maksimum hýzýna göre ses seviyesini ve tizliðini ayarla
        carEngineAS.pitch = carPitchAnimationCurve.Evaluate(carMaxSpeedPercentage);


        
        if (input.y < 0 && carMaxSpeedPercentage > 0.2f) // Eðer fren yapýlýyorsa ve hýz %20'den fazlaysa
        {
            // Fren sesi çal
            if (!carSkidAS.isPlaying)
                carSkidAS.Play();

            // Fren sesi için ses seviyesini ayarla
            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 1.0f, Time.deltaTime * 10);

        }
        else
        {
            // Fren sesini durdur
            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 0 , Time.deltaTime * 30);
        }
    }

    // Maksimum hýzý ayarla
    public void SetMaxSpeed(float newMaxSpeed)
    {
        maxForwardVelocity = newMaxSpeed;
    }

    //Araba çarptý mý kontrol et
    public bool GetIsCrashed()
    {
        return isCrashed;
    }

    //Araba çarptýysa durumu ayarla
    public void SetIsCrashed(bool crashed)
    {
        isCrashed = crashed;
    }


    private void OnCollisionEnter(Collision collision)
    {
        //Çarptýðý nesne baþka bir araba ise
        if (collision.transform.root.CompareTag("Car AI"))
        {
           
            Vector3 velocity = rb.linearVelocity; // Çarpma anýndaki hýz vektörünü al

            crashHandler.Crash(velocity * 45); // Çarpma kuvvetini uygula

            isCrashed = true; // Arabanýn çarptýðýný belirt
        }
    }

    //Arabanýn durumunu sýfýrla
    public void ResetCar()
    {
        isCrashed = false; // Arabanýn çarpma durumunu sýfýrla
        input = Vector2.zero; // Kuvveti sýfýrla

        rb.linearVelocity = Vector3.zero; // Hýzý sýfýrla
        rb.angularVelocity = Vector3.zero; // Açýsal hýzý sýfýrla
    }
}
