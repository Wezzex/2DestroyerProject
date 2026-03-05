using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour, IDamagable
{
    [SerializeField] private ShipController shipController;


    [SerializeField] private int health;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected bool bIsDead = false;


    public int Health 
    {
        get => health;
        set => health = value; 
    }

    private void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {

        health -= amount;

        if (health <= 0)
        {
            StartCoroutine(Dead());
        }


    }

    private IEnumerator Dead()
    {

        // Disable Movement and Shooting
        shipController.HandleMoveShip(Vector2.zero);
        shipController.SetShootingState(false);

        // Play Explotion Vfx and Sfx

        // Notify GameManager, Spawner

        // Destroy or Disable
        Destroy(gameObject);

        yield return null;

    }


}
