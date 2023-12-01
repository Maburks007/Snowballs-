using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
    public Camera[] cameras;
    public Transform currentCamera;

    public GameObject[] menuGroups;
    GameObject[] sizeUI;
    // Start is called before the first frame update
    void Start()
    {
        sizeUI = GameObject.FindGameObjectsWithTag("SizeUI");

        SelectCamera(0);
    }

    bool selectingNewCamera = false;
    // Update is called once per frame
    void Update()
    {
        if (!selectingNewCamera)
        {
            StartCoroutine(NewCamera());
        }

        LookAtCamera();
    }

    IEnumerator NewCamera()
    {
        selectingNewCamera = true;
        yield return new WaitForSeconds(Random.Range(3f, 9f));

        SelectCamera(Random.Range(0, cameras.Length - 1));
        selectingNewCamera = false;
    }

    void LookAtCamera()
    {
        foreach (GameObject sui in sizeUI)
        {
            sui.transform.parent.LookAt(currentCamera);
        }
    }

    void SelectCamera(int camera)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = false;
        }

        cameras[camera].enabled = true;
        currentCamera = cameras[camera].transform;
    }



    #region Menu UI

    public void ChangeGroup(int group)
    {
        foreach (GameObject menuGroup in menuGroups)
        {
            menuGroup.SetActive(false);
        }

        menuGroups[group].SetActive(true);
    }


    public void PlayGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Arena");
    }


    public void QuitGame()
    {
        Application.Quit();
    }   
    #endregion
}
