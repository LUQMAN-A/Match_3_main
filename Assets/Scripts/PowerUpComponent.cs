using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public enum PowerUp
{
    Freeze,
    Bomb
}

public class PowerUpComponent : MonoBehaviour
{
    public PowerUp PowerUp;
}
