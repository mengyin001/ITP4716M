using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour {

	public void NextScene (string sceneName) {
        SceneLoader.targetScene = sceneName;
        SceneManager.LoadScene("LoadingScence");
    }
}


