using System.Collections;
using System.Runtime.CompilerServices;
using UnityEditor.Build.Content;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    private void Awake()
    {
        currentHealth = maxHealth; // start at full health
    }
    // call to deal damage
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log(gameObject.name + "-" + amount + ". Current HP:" + currentHealth); 

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    private void Die()
    { 
        Debug.Log(gameObject.name + " has been defeated");

        SceneLoader.Instance.enemyDefeated = true; // set global flag that the enemy was defeated.
        
        Destroy(gameObject); // destroys the enemy.
        SceneLoader.Instance.LoadExploration();
    }
}