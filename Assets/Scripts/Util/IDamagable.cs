using UnityEngine;

public interface IDamagable 
{
    int Health { get; set; }
    void TakeDamage(int amount);
}
