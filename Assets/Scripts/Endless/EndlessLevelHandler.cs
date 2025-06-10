using System.Collections;
using UnityEngine;

public class EndlessLevel : MonoBehaviour
{
    [SerializeField]
    GameObject[] sectionsPrefabs;

    GameObject[] sectionsPool = new GameObject[20];

    GameObject[] sections = new GameObject[10];

    Transform playerCarTransform;

    WaitForSeconds waitFor100ms = new WaitForSeconds(0.1f);

    const float sectionLength = 26;
    void Start()
    {
        playerCarTransform = GameObject.FindGameObjectWithTag("Player").transform;

        int preFabIndex = 0;

        for (int i = 0; i < sectionsPool.Length; i++)
        {
            sectionsPool[i] = Instantiate(sectionsPrefabs[preFabIndex]);
            sectionsPool[i].SetActive(false);

            preFabIndex++;

            if (preFabIndex > sectionsPrefabs.Length - 1)
                preFabIndex = 0;
        }

        for (int i = 0; i < sections.Length; i++)
        {
            GameObject randomSection = GetRandomSectionFromPool();

            randomSection.transform.position = new Vector3(sectionsPool[i].transform.position.x, -100, i * sectionLength);
            //randomSection.transform.position = new Vector3(0, 0, i * sectionLength);
            randomSection.SetActive(true);

            sections[i] = randomSection;
        }
        StartCoroutine(UpdateLessOftenCO());
    }

    IEnumerator UpdateLessOftenCO()
    {
        while (true)
        {
            UpdateSectionPosition();
            yield return waitFor100ms;
        }
    }

    void UpdateSectionPosition()
    {
        for (int i = 0; i < sections.Length; i++)
        {
            if (sections[i].transform.position.z - playerCarTransform.position.z < -sectionLength)
            //if (playerCarTransform.position.z - sections[i].transform.position.z > sectionLength)
            {
                Vector3 lastSectionPosition = sections[i].transform.position;
                sections[i].SetActive(false);

                //sections[i] = GetRandomSectionFromPool();
                //sections[i].transform.position = new Vector3(lastSectionPosition.x, -100, lastSectionPosition.z + sectionLength * sections.Length);

                GameObject newSection = GetRandomSectionFromPool();

                if (newSection != null)
                {
                    //newSection.transform.position = new Vector3(0, -100, lastSectionPosition.z + sectionLength * sections.Length);
                    newSection.transform.position = new Vector3(lastSectionPosition.x, -100, lastSectionPosition.z + sectionLength * sections.Length);
                    newSection.SetActive(true);
                    sections[i] = newSection;
                }
                else
                {
                    Debug.LogWarning("No new section could be assigned.");
                }

            }
        }
    }

    GameObject GetRandomSectionFromPool()
    {
        int randomIndex = Random.Range(0, sectionsPool.Length);

        bool isNewSectionFound = false;

        while (!isNewSectionFound)
        {
            if (!sectionsPool[randomIndex].activeInHierarchy)
                isNewSectionFound = true;
            else
            {
                randomIndex++;

                if (randomIndex > sectionsPool.Length - 1)
                    randomIndex = 0;
            }
        }

        return sectionsPool[randomIndex];
    }

  
}
