
using UnityEngine;
using UnityEngine.SceneManagement;

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    CarHandler carHandler;

    [Header("SFX")]
    [SerializeField]
    AudioSource honkHornAS;

    private void Awake()
    {
        if (!CompareTag("Player"))
        {
            Destroy(this);
            return;
        }
    }

    void Update()
    {
        Vector2 input = Vector2.zero;

        input.x = Input.GetAxis("Horizontal");
        input.y = Input.GetAxis("Vertical");

        carHandler.SetInput(input);

        if (Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1.0f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        }

        if (Input.GetKey(KeyCode.F))
        {
            if (!honkHornAS.isPlaying)
            {
                honkHornAS.Play();
            }
        }
        else
        {
            if (honkHornAS.isPlaying)
            {
                honkHornAS.Stop();
            }
        }
    }
}
