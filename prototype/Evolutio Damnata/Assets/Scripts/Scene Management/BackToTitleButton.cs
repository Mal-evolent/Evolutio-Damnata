using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitleButton : MonoBehaviour
{
    /**
     * This function is called when the object becomes enabled and active.
     */


    public void onClick()
    {
        SceneManager.LoadScene("menuScene");
    }
}
