using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUpEffectScript : MonoBehaviour
{
    public PowerUp powerUp;
    public bool freezeEnd;
    [SerializeField] private Animator animator;
    public void OnAnimationEnd()
    {       
        Destroy(gameObject);
    }

    public void Initialize(PowerUp type, bool freezeEnding = false)
    {
        powerUp = type;
        freezeEnd = freezeEnding;
        switch (powerUp)
        {
            case PowerUp.Freeze:
                if (!freezeEnd)
                    animator.SetTrigger("Freeze Time");
                else animator.SetTrigger("Unfreeze Time");
                break;
            case PowerUp.Bomb:
                animator.SetTrigger("Bomb Explosion");
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
