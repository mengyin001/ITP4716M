using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void GoToTeachLevel()
    {
        SceneManager.LoadScene("Teachlevel");
    }
   
}
