using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
 * This class is used to control the main menu of the game.
 * It contains two methods, one to quit the game and one to start the game.
 */

public class MainMenuScript : MonoBehaviour
{

    public static void quitGame() {
        Application.Quit(1);
    }
    public static void StartGame() {
        SceneManager.LoadScene(sceneName: "gameScene");

    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
