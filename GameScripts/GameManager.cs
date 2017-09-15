using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameScripts
{
	public class GameManager : MonoBehaviour
	{	
		private GameObject bar;
		private ProgressBar progress;
		private Scene activeScene;
		private bool isMenuActive = false;
		private GameObject[] buttons;
		
		private void Awake()
		{
			activeScene = SceneManager.GetActiveScene();
			buttons	= GameObject.FindGameObjectsWithTag("Button");
			if (activeScene.buildIndex == 0)
			{
				bar = GameObject.Find("Progress Bar");
				bar.SetActive(false);
				progress = bar.GetComponentInChildren<ProgressBar>();
			}
			else
			{
				ManageGUI();
				Debug.Log("Active Scene: " + activeScene.buildIndex);
			}
		}

		public void NewGame()
		{
			StartCoroutine(LoadScene());
		}

		private IEnumerator LoadScene()
		{		
			ManageGUI();
			yield return new WaitForSeconds(1);
			AsyncOperation scene = SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);	
			scene.allowSceneActivation = false;
			while (!scene.isDone)
			{
				progress.Value += 0.1f;
				if (scene.progress == 0.9f)
				{
					progress.Value = 1f;
					scene.allowSceneActivation = true;
				}			
				yield return null;
			}	
		
		}

		private void ManageGUI()
		{
			HideButtons();
			if (activeScene.buildIndex == 0)
			{
				bar.SetActive(true);
			}
		}

		public void Quit()
		{
			Application.Quit();
		}

		private void FixedUpdate()
		{
			if (activeScene.buildIndex == 1 && Input.GetButtonDown("Cancel") && !isMenuActive)
			{
				Time.timeScale = 0;				
				IngameMenu();				
			}
		}

		private void IngameMenu()
		{			 
			ShowButtons();
			MouseLook(false);
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.Confined;
			if (!isMenuActive)
			{
				isMenuActive = true;
			}
		}

		public void Resume()
		{
			if (isMenuActive)
			{
				HideButtons();
				Cursor.visible = false;
				MouseLook(true);
				Time.timeScale = 1.0f;
				isMenuActive = false;
			}
		}

		private void ShowButtons()
		{
			foreach (var button in buttons)
			{
				button.SetActive(true);
			}
		}

		private void HideButtons()
		{
			foreach (var button in buttons)
			{
				button.SetActive(false);
			}
		}

		private void MouseLook(bool value)
		{
			GameObject.Find("Player").GetComponent<FPSController>().setLookAlloowed(value);
		}
	}
}
