using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour {

	public void PauseGame () {
		Time.timeScale = 0;
	}

	public void ResumeGame () {
		Time.timeScale = 1;
	}

	public void ExitGame () {
		Time.timeScale = 1;
		SceneManager.LoadScene("title");
	}
}
