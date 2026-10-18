using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UnitManager : MonoBehaviour, IDamagable
{

    [Header("Health Settings")]
    [SerializeField] private  int health;
    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected bool bIsDead = false;

    // Raised the moment a unit starts dying. Spawners, owners and the ThreatMeter listen to these.
    public event Action<UnitManager> Died;
    // Raised on every hit (unit, damage). SubLightDrive uses it to drain its charge.
    public event Action<UnitManager, int> Damaged;
    public static event Action<UnitManager> AnyUnitDied;

    // Every enabled unit. SubLightDrive checks it to drop out before reaching a station or destroyer.
    private static readonly List<UnitManager> active = new List<UnitManager>();
    public static IReadOnlyList<UnitManager> Active => active;

    // Hostile sub-light drives drop out before getting closer than this (0 = no need to keep clear).
    public virtual float SubLightClearance => 0f;

    protected virtual void OnEnable() => active.Add(this);
    protected virtual void OnDisable() => active.Remove(this);

    public bool IsDead => bIsDead;
    public int MaxHealth => maxHealth;
    public float HealthNormalized => maxHealth > 0 ? (float)health / maxHealth : 0f;

    public void Repair()
    {
        if (bIsDead) return;
        health = maxHealth;
    }

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
        Damaged?.Invoke(this, amount);

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

        Died?.Invoke(this);
        AnyUnitDied?.Invoke(this);

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
