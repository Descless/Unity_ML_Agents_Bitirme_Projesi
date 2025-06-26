using UnityEngine;

public class MLStats : MonoBehaviour
{
    [SerializeField] private CarRLAgent _carRLAgent; // CarRLAgent referans�

    private GUIStyle _deafult = new GUIStyle();
    void Start()
    {
        _deafult.fontSize = 40;
        _deafult.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        // CurrentEpisode ve CumulativeReward de�erlerini ekrana yazd�rma
        GUI.Label(new Rect(10, 10, 300, 30), "B�l�m : " + _carRLAgent.CurrentEpisode + " - Ad�m Say�s�: " + _carRLAgent.StepCount, _deafult);
        GUI.Label(new Rect(10, 40, 300, 30), "Toplam �d�l: " + _carRLAgent.CumulativeReward.ToString(), _deafult);
    }


    void Update()
    {
        
    }
}
