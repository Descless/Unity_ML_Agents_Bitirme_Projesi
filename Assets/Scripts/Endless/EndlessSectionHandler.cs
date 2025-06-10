using UnityEngine;

public class EndlessSectionHandler : MonoBehaviour
{
    Transform playerCarTransfrom;

    void Start()
    {
        playerCarTransfrom = GameObject.FindGameObjectWithTag("Player").transform;

    }

    private void Update()
    {
        float distancePlayer = transform.position.z - playerCarTransfrom.position.z;

        float lerpPercentage = 1.0f - ((distancePlayer -100)  / 150.0f);
        lerpPercentage = Mathf.Clamp01(lerpPercentage);

        transform.position = Vector3.Lerp(new Vector3(transform.position.x, -10, transform.position.z), new Vector3(transform.position.x, 0, transform.position.z), lerpPercentage);


    }

}
