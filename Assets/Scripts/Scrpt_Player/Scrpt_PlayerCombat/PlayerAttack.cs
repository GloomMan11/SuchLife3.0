using Unity.VisualScripting;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Animator animator;
    // [SerializeField] private playerInfo SelfInfo;

    [Header("Sword Settings")]
    [SerializeField] private float swordDamageMult = 1.5f;
    
    
    private float _lastAttackTime;
    private float additiveDamageBoost = 0f;


    private void Update()
    {
        if (InputHandler.Instance == null)
        {
            Debug.LogError("InputHandler.Instance is null!");
            return;
        }

        if (InputHandler.Instance.IsAttacking && Time.time >= _lastAttackTime + attackCooldown)
        {
            //if holding a weapon, use that instead
            if (InputHandler.currSelectedContext == InputHandler.SelectedContext.Weapon) 
            {
                // if (SelfInfo.HeldItem.itemName == "Sword")
                // {
                StartSwordAttack();
                // }
            } else
            {
                StartAttack();

            }
        }
    }

    private void StartAttack()
    {
        _lastAttackTime = Time.time;
        
        animator.SetTrigger("Attack");

       Invoke("DetectHits", 0.5f);
    }

    public void EndAttack() 
    {

    }

    private void StartSwordAttack()
    {
        _lastAttackTime = Time.time;
        animator.SetTrigger("Sword");
        Invoke("DetectSwordHits", 0.5f);
    }

    public void ApplyDamageBoost(float amount, float duration)
    {
        additiveDamageBoost = amount;
        Debug.Log($"Applied +{amount} damage boost for {duration} seconds.");
        Invoke(nameof(RemoveDamageBoost), duration);
    }

    private void RemoveDamageBoost()
    {
        additiveDamageBoost = 0f;
        Debug.Log("Damage boost ended.");
    }

    private void DetectHits()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position, 
            attackRange
        );

        ApplyHits(hitEnemies, attackDamage);
    }

    private void DetectSwordHits()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position, 
            attackRange + 1
        );

        int swordDamage = Mathf.RoundToInt(attackDamage * swordDamageMult);
        ApplyHits(hitEnemies, swordDamage);
    }

    private void ApplyHits(Collider2D[] hitEnemies, int baseDamage)
    {
        foreach (Collider2D enemy in hitEnemies)
        {
            // ignore triggers and non-enemy layers
            if (enemy.isTrigger) continue;
            //only attack living entities
            if (enemy.TryGetComponent<Mob>(out var mob))
            {
                if (enemy.TryGetComponent<Health>(out var health))
                {
                    int boostedDamage = Mathf.RoundToInt(baseDamage + additiveDamageBoost);
                    health.TakeDamage(boostedDamage, true, gameObject);
                }
                //apply a knockback force to the mob
                //read force from some value associated with the attack or weapon
                float knockbackForce = 10f;
                Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
                Vector2 knockbackVector = knockbackDir * knockbackForce;
                mob.applyKnockback(knockbackVector, knockbackForce);
                //Debug.Log("push");
                //Debug.Log(knockbackVector);
            }         
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}