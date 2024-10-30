using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuSwitcher : MonoBehaviour
{
    [SerializeField]
    private Animator freezeAnimator, bombAnimator;


    [SerializeField]
    private Text text;

    [SerializeField]
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        text.text = PlayerPrefs.GetInt("Highscore", 0).ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SelectPowerup(PowerUpComponent powerUp)
    {
        PersistentData.PowerUp = powerUp.PowerUp;

        if (powerUp.PowerUp == PowerUp.Freeze)
        {
            freezeAnimator.SetBool("NotSelected", false);
            bombAnimator.SetBool("NotSelected", true);
        }
        else
        {
            freezeAnimator.SetBool("NotSelected", true);
            bombAnimator.SetBool("NotSelected", false);
        }
    }


    public void StartGame()
    {
        animator.SetTrigger("Play");
    }
    public void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }
}
