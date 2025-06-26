using UnityEngine;

public class CrashHandler : MonoBehaviour
{
    [SerializeField]
    GameObject orgObject;

    [SerializeField]
    GameObject model;

    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void Crash(Vector3 externalForce)
    {
        print("CRASHED!");

        //orgObject.SetActive(false);

        rb.transform.parent = null;

        //rb.gameObject.SetActive(true);
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.AddForce(Vector3.right * 100 + externalForce, ForceMode.Force);
        rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
    }
}
