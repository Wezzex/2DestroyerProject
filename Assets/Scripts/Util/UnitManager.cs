using System.Collections;
using UnityEngine;

public abstract class UnitManager : MonoBehaviour, IDamagable
{

    [Header("Health Settings")]
    [SerializeField] private  int health;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected bool bIsDead = false;

    [SerializeField] private GameObject explotionPrefab;

    public int Health
    {
        get => health;
        set => health = value;
    }
    private void Start()
    {
        health = maxHealth;
    }

    public virtual void TakeDamage(int amount)
    {

        health -= amount;

        if (health <= 0 && !bIsDead)
        {
            StartCoroutine(DeathCoroutine());
        }


    }

    public virtual void CreateDeathExplotion()
    {
        Vector3 explotionSpawnPosition = this.transform.position;
        GameObject explotion = Instantiate(explotionPrefab, explotionSpawnPosition, Quaternion.identity);

    }

    protected virtual void OnDeathStarted()
    {
        
    }

    protected virtual void OnDeathFinished()
    {

    }
    protected virtual IEnumerator DeathCoroutine()
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
