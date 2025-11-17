using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController : MonoBehaviour
{
    public  Character data;

    public abstract void TakeDamage(int damage);
    public abstract bool isPlayer { get; }
    public abstract bool isDead { get; }
}
