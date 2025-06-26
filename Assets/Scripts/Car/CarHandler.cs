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

        carEngineAS.Play(); // Araba motor sesini ba�lat

        carStartPositionZ = transform.position.z; // Araban�n ba�lang�� z pozisyonunu al

    }

  
    void Update()
    {
        SetInput(new Vector2(0,30.0f));
        gameModel.transform.rotation = Quaternion.Euler(0, rb.linearVelocity.x * 5, 0); // Araba modelini d�nd�r

        UpdateCarAudio(); // Araba sesini g�ncelle

        distanceTravelled = transform.position.z - carStartPositionZ; // Araban�n ne kadar mesafe katetti�ini hesapla
    }

    private void FixedUpdate()
    {
        //Maksimum h�za kadar h�zlanmay� sa�l�yor
        if (rb.linearVelocity.z >= maxForwardVelocity)
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, maxForwardVelocity);

        //Araba �arpt�ysa
        if (isCrashed)
        {
            rb.linearDamping = rb.linearVelocity.z * 0.1f;
            rb.linearDamping = Mathf.Clamp(rb.linearDamping, 1.5f, 10);

            rb.MovePosition(Vector3.Lerp(transform.position, new Vector3(0,0,transform.position.z), Time.deltaTime * 0.5f)); //Arabay� savur

            return;
        }
        //Arabaya g�� uyguland��� zaman ileri hareket ettir
        if (input.y > 0)
            Accelerate();
        else
            rb.linearDamping = 0.2f;
        //Arabaya g�� uygulanmad�ysa yava�lat
        if (input.y < 0)
            Brake();
        //Arabay� sa�a veya sola hareket ettir
        Steer();

        //Geriye do�ru hareket etmesini engelle
        if (rb.linearVelocity.z <= 0)
            rb.linearVelocity = Vector3.zero;
    }

    public void Accelerate()
    {
        rb.linearDamping = 0;

        //Maksimum h�za ula�t�ysa h�zlanmay� engelle
        if (rb.linearVelocity.z >= maxForwardVelocity)
            return;

        //H�zlanma kuvveti uygula
        rb.AddForce(rb.transform.forward * accelerationMultiplier * input.y);
    }

    void Brake()
    {
        //E�er araba geri hareket ediyorsa, geri hareket etmesini engelle
        if (rb.linearVelocity.z <= 0)
            return;

        //E�er araba ileri hareket ediyorsa, fren kuvveti uygula
        rb.AddForce(rb.transform.forward * breakMultiplier * input.y);
    }

    public void Steer()
    {

       
        if (Mathf.Abs(input.x)>0)
        {
            
            float speedBaseSteerLimit = 1.0f;

            //D�nme h�z� i�in maksimum de�eri ayarla
            speedBaseSteerLimit = Mathf.Clamp01(speedBaseSteerLimit);

            // E�er araba sa�a veya sola hareket ediyorsa, sa�a veya sola kuvvet uygula
            rb.AddForce(rb.transform.right * steeringMultiplier * input.x * speedBaseSteerLimit);

            // Maksimum d�nme h�z�na g�re normalize et
            float normalizedX = rb.linearVelocity.x / maxSteerVelocity;

            // Normalize edilmi� de�eri -1 ile 1 aras�nda s�n�rla
            normalizedX = Mathf.Clamp(normalizedX, -1.0f, 1.0f);


            // Araban�n x eksenindeki h�z�n�, maksimum direksiyon h�z� ile �arp ve yeni bir h�z vekt�r� olu�tur
            rb.linearVelocity = new Vector3(normalizedX * maxSteerVelocity, 0, rb.linearVelocity.z);
        }

        else
        {
            //auto center
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, new Vector3(0, 0, rb.linearVelocity.z), Time.fixedDeltaTime * 3);
        
        }
    }

    //Kuvvet de�erini ayarla
    public void SetInput(Vector2 inputVector)
    {
       
        inputVector.Normalize(); // Normalize et ki de�erler -1 ile 1 aras�nda olsun
        input = inputVector;
    }

    //Araba sesini g�ncelle
    void UpdateCarAudio()
    {
        // Araban�n maksimum h�z�na g�re normalize et
        float carMaxSpeedPercentage = rb.linearVelocity.z / maxForwardVelocity;

        // Araban�n maksimum h�z�na g�re ses seviyesini ve tizli�ini ayarla
        carEngineAS.pitch = carPitchAnimationCurve.Evaluate(carMaxSpeedPercentage);


        
        if (input.y < 0 && carMaxSpeedPercentage > 0.2f) // E�er fren yap�l�yorsa ve h�z %20'den fazlaysa
        {
            // Fren sesi �al
            if (!carSkidAS.isPlaying)
                carSkidAS.Play();

            // Fren sesi i�in ses seviyesini ayarla
            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 1.0f, Time.deltaTime * 10);

        }
        else
        {
            // Fren sesini durdur
            carSkidAS.volume = Mathf.Lerp(carSkidAS.volume, 0 , Time.deltaTime * 30);
        }
    }

    // Maksimum h�z� ayarla
    public void SetMaxSpeed(float newMaxSpeed)
    {
        maxForwardVelocity = newMaxSpeed;
    }

    //Araba �arpt� m� kontrol et
    public bool GetIsCrashed()
    {
        return isCrashed;
    }

    //Araba �arpt�ysa durumu ayarla
    public void SetIsCrashed(bool crashed)
    {
        isCrashed = crashed;
    }


    private void OnCollisionEnter(Collision collision)
    {
        //�arpt��� nesne ba�ka bir araba ise
        if (collision.transform.root.CompareTag("Car AI"))
        {
           
            Vector3 velocity = rb.linearVelocity; // �arpma an�ndaki h�z vekt�r�n� al

            crashHandler.Crash(velocity * 45); // �arpma kuvvetini uygula

            isCrashed = true; // Araban�n �arpt���n� belirt
        }
    }

    //Araban�n durumunu s�f�rla
    public void ResetCar()
    {
        isCrashed = false; // Araban�n �arpma durumunu s�f�rla
        input = Vector2.zero; // Kuvveti s�f�rla

        rb.linearVelocity = Vector3.zero; // H�z� s�f�rla
        rb.angularVelocity = Vector3.zero; // A��sal h�z� s�f�rla
    }
}
