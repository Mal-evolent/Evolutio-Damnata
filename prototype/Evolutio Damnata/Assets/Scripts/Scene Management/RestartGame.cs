using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartGame : MonoBehaviour
{
    public void onClick()
    {
        // Keep the GameStateManager's saved player health
        // No need to reset it as we want to carry it over
        Debug.Log("[RestartGame] Loading game scene with saved player health");

        SceneManager.LoadScene("gameScene");
    }
}
