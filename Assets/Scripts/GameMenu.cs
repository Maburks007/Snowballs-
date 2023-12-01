using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenu : MonoBehaviour
{

    public GameObject menuCanvas;
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            menuCanvas.SetActive(!menuCanvas.activeInHierarchy);

        if (Input.GetKeyDown(KeyCode.R))
            RestartGame();
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("Main Menu");
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("Arena");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
