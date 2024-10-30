using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuScript : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private GameController gameController;

    private AsyncOperation sceneLoader;

    private void Start()
    {
        sceneLoader = null;
    }

    public void Pause()
    {
        gameController.PauseGame(true);
        animator.SetTrigger("Pause");
    }

    public void Unpause()
    {
        animator.SetTrigger("Unpause");
    }

    public void OnUnpauseEnded()
    {
        if (sceneLoader != null)
            sceneLoader.allowSceneActivation = true;
        else
            gameController.PauseGame(false);

    }

    public void Restart()
    {
        Unpause();
        sceneLoader = SceneManager.LoadSceneAsync("GameScene");
        sceneLoader.allowSceneActivation = false;
    }

    public void Quit()
    {
        Unpause();
        sceneLoader = SceneManager.LoadSceneAsync("MainMenu");
        sceneLoader.allowSceneActivation = false;
    }

}
