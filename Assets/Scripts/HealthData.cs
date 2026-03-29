using UnityEngine;
using System;

[System.Serializable]
public class HealthData
{
    public float MaxHealth { get; private set; }

    private float currentHealth;

    public bool IsDead { get; private set; }
    public float CurrentHealth
    {
        get => currentHealth;
        private set
        {
            currentHealth = Mathf.Clamp(value, 0f, MaxHealth);

            if (currentHealth <= 0f && !IsDead)
            {
                IsDead = true;
            }
        }
    }

    public float Defense { get; private set; }
    public float RegenRate { get; private set; }

    public HealthData(float maxHealth, float defense = 0, float regenRate = 0)
    {
        MaxHealth = maxHealth;
        Defense = defense;
        RegenRate = regenRate;
        CurrentHealth = maxHealth;
        IsDead = false;
    }

    public float TakeDamage(float damage)
    {
        float finalDamage = Mathf.Max(damage - Defense, 1f);
        CurrentHealth -= finalDamage;

        return finalDamage;
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
    }

    public void Regenerate(float dTime)
    {
        if (!IsDead && RegenRate > 0f)
        {
            Heal(dTime * RegenRate);
        }
    }
    
    public void Reset()
    {
        CurrentHealth = MaxHealth;
        IsDead= false;
    }
}
public struct KnockbackData
{
    public Vector2 direction;
    public float force;

    public KnockbackData(Vector2 dir, float f)
    {
        direction = dir;
        force = f;
    }
}
