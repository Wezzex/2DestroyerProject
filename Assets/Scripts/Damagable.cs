using UnityEngine;
using UnityEngine.Events;

public class Damagable : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health;

  

    public UnityEvent OnDead;
    public UnityEvent<float> OnHealthChange;
    public UnityEvent OnHit, OnHeal;

    public int Health 
    { get { return health; }
        set
        { 
            health = value; 
            OnHealthChange?.Invoke((float) Health / maxHealth);
        }
    }

    private void Start()
    {
        Health = maxHealth;
    }

    internal void Hit(int damageValue)
    {
        Health -= damageValue;
        if (health <= 0)
        {
            OnDead?.Invoke();
        }
        else
        {
            OnHit?.Invoke();
        }
        
    }

    internal void Heal(int healValue)
    {
        Health += healValue;
        Health = Mathf.Clamp(Health, 0, maxHealth);
        OnHeal?.Invoke();
    }

}
