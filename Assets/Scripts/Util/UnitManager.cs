using System.Collections;
using UnityEngine;

public abstract class UnitManager : MonoBehaviour, IDamagable
{

    [Header("Health Settings")]
    [SerializeField] private  int health;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected bool bIsDead = false;


    public bool IsDead => bIsDead;

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

    }

    public virtual void OnDestroyedBegin()
    {

    }

    public virtual void OnDestroyedEnd()
    { 
    
    }
    protected virtual IEnumerator DeathCoroutine()
    {
        bIsDead = true;

        // Disable Movement and Shooting


        // Play Explotion Vfx and Sfx

        CreateDeathExplotion();

        yield return new WaitForSeconds(0.4f);

        // Notify GameManager, Spawner
        OnDestroyedEnd();

        // Destroy or Disable
        gameObject.SetActive(false);



        yield return null;

    }
}
