using UnityEngine;

public class MLStats : MonoBehaviour
{
    [SerializeField] private CarRLAgent _carRLAgent; // CarRLAgent referansý

    private GUIStyle _deafult = new GUIStyle();
    void Start()
    {
        _deafult.fontSize = 40;
        _deafult.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        // CurrentEpisode ve CumulativeReward deðerlerini ekrana yazdýrma
        GUI.Label(new Rect(10, 10, 300, 30), "Bölüm : " + _carRLAgent.CurrentEpisode + " - Adým Sayýsý: " + _carRLAgent.StepCount, _deafult);
        GUI.Label(new Rect(10, 40, 300, 30), "Toplam Ödül: " + _carRLAgent.CumulativeReward.ToString(), _deafult);
    }


    void Update()
    {
        
    }
}
