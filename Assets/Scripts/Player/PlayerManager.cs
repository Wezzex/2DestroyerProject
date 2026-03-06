using System.Collections;
using UnityEngine;

public class PlayerManager : MonoBehaviour, IDamagable
{
    [Header("References")]
    [SerializeField] private ShipController shipController;

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

    private void CreateDeathExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }

    private IEnumerator Dead()
    {
        bIsDead = true;
        
        // Disable Movement and Shooting
        

        // Play Explotion Vfx and Sfx

        CreateDeathExplotion();

        yield return new WaitForSeconds(0.2f);

        // Notify GameManager, Spawner

        // Destroy or Disable
        GameManager.Instance.RequestGameOver(GameManager.GameOverReason.PlayerDied);
        gameObject.SetActive(false);



        yield return null;

    }
}
