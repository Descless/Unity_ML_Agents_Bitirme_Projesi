using UnityEngine;
using TMPro;
public class UIHandler : MonoBehaviour
{

    [SerializeField]
    TextMeshProUGUI distanceTravelledText;

    CarHandler playerCarHandler;


    private void Awake()
    {
        playerCarHandler = GameObject.FindGameObjectWithTag("Player").GetComponent<CarHandler>();
    }
    void Start()
    {
        
    }

    void Update()
    {
        distanceTravelledText.text = playerCarHandler.DistanceTravelled.ToString("000000");
    }
}
