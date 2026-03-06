using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour, IDamagable
{

    [Header("References")]
    [SerializeField] private ShipController shipController;
    [SerializeField] private EnemySpawner spawner;

    [Header("Health Settings")]
    [SerializeField] private int health;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected bool bIsDead = false;

    [SerializeField] private GameObject explotionPrefab;
    public bool IsDead => bIsDead;


    public int Health 
    {
        get => health;
        set => health = value; 
    }

    private void Awake()
    {
        shipController = GetComponent<ShipController>();
        spawner = GetComponent<EnemySpawner>();
    }

    private void Start()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {

        health -= amount;

        if (health <= 0 && !bIsDead)
        {
            StartCoroutine(Dead());
        }


    }

    void CreateExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }

    private IEnumerator Dead()
    {
        bIsDead = true;

        // Disable Movement and Shooting
        shipController.HandleMoveShip(Vector2.zero);
        shipController.SetShootingState(false);

        // Play Explotion Vfx and Sfx

        CreateExplotion();

        yield return new WaitForSeconds(0.5f);

        // Notify GameManager, Spawner

        // Destroy or Disable
        Destroy(gameObject);

        yield return null;

    }


}
