using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string SceneName;
    
    public void LoadLevel()
    {
        SceneManager.LoadScene(SceneName);
    }
    public void Quit()
    {
        Application.Quit();
    }

}
