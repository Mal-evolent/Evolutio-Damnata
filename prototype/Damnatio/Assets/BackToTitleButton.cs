using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToTitleButton : MonoBehaviour
{
    public void onClick()
    {
        SceneManager.LoadScene("menuScene");
    }
}
