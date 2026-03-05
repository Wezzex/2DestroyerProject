using UnityEngine;

public class PlayerManager : MonoBehaviour, IDamagable
{
    [SerializeField] private int health = 100;

    public int Health
    {
        get => health;
        set => health = value;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
    }
}
