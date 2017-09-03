using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitGame : MonoBehaviour
{
    public void HelpBack()
    {
        SceneManager.LoadScene("Menu");
    }
}
