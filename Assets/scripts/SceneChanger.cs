using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string ScenName;
    public void GoToScenName()
    {
        SceneManager.LoadScene(ScenName);
    }
   
}
