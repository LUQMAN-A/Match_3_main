using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    [SerializeField]
    float matchTime;

    [SerializeField]
    float comboMultiplierAddition;
    [SerializeField]
    float comboFalloff;

    [SerializeField]
    Animator animator;

    [SerializeField]
    Text timerText;
    [SerializeField]
    Text ScoreText;

    [SerializeField]
    Canvas gameCanvas;

    [SerializeField]
    Animator gameOverAnimator;

    [SerializeField]
    GameObject powerUpEffectPrefab;

    private float timer;
    private bool timerPaused;
    private float score;
    private float comboMultiplier;
    private float comboTimer;

    private float freezeTime;

    private PowerUpEffectScript activeFreeze;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(PersistentData.PowerUp);
        animator.SetTrigger("Enter Level");

        timer = matchTime;
        score = 0f;
        comboMultiplier = 1f;
        comboTimer = 0f;
        freezeTime = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (!(timerPaused || freezeTime > 0f))
        {
            timer -= Time.deltaTime;
        }
        if (timer <= 0f && !timerPaused)
        {
            TriggerGameOver();
        }
        if (freezeTime > 0f && !timerPaused)
        {
            freezeTime -= Time.deltaTime;
            if (freezeTime <= 0f)
            {
                freezeTime = 0f;
                if (activeFreeze == null)
                {
                    GameObject obj = Instantiate(powerUpEffectPrefab, gameCanvas.transform);
                    activeFreeze = obj.GetComponent<PowerUpEffectScript>();
                }
                activeFreeze.Initialize(PowerUp.Freeze, true);
            }
        }
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
        }
        int intScore = Mathf.FloorToInt(score);
        ScoreText.text = intScore.ToString();
        timerText.text = new TimeSpan(0,0, (int)timer).ToString(@"mm\:ss");
    }

    public void PauseGame(bool pause)
    {
        timerPaused = pause;
    }

    public void FreezeTime(float time)
    {
        freezeTime += time;
        if (activeFreeze == null)
        {
            GameObject obj = Instantiate(powerUpEffectPrefab, gameCanvas.transform);
            activeFreeze = obj.GetComponent<PowerUpEffectScript>();
            activeFreeze.Initialize(PowerUp.Freeze);
        }
        else if (activeFreeze.freezeEnd) activeFreeze.Initialize(PowerUp.Freeze);

    }

    public void TriggerExplosionAtLocation(Vector3 pos)
    {
        GameObject obj = Instantiate(powerUpEffectPrefab, gameCanvas.transform);
        obj.transform.position = pos;
        PowerUpEffectScript explosionScript = obj.GetComponent<PowerUpEffectScript>();
        explosionScript.Initialize(PowerUp.Bomb);
    }

    public void AddScore(int count)
    {
        if (comboTimer <= 0f)
        {
            comboMultiplier = 1f;
        }
        else
        {
            comboMultiplier += comboMultiplierAddition;
        }
        comboTimer = comboFalloff;
        float pointsToAdd = count * comboMultiplier;
        score += pointsToAdd;

        //Debug.Log($"Added {count} * {comboMultiplier} = {pointsToAdd}");
    }

    public void TriggerGameOver()
    {
        Debug.Log("game over");
        int intScore = Mathf.FloorToInt(score);
        int highscore = PlayerPrefs.GetInt("Highscore", 0);
        if (intScore > highscore)
            PlayerPrefs.SetInt("Highscore", intScore);
        timerPaused = true;

        gameOverAnimator.SetTrigger("Pause");
    }
}
