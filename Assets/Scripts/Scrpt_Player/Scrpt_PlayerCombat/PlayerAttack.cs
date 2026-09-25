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
    [SerializeField] private PlayerInfo SelfInfo;

    [Header("Sword Settings")]
    [SerializeField] private float swordDamageMult = 1.5f;
    
    [Header("Crossbow Settings")]
    [SerializeField] private float crossbowOffset; // Default for weapon facing up = -90
    [SerializeField] private GameObject crossbowProjectile;
    [SerializeField] private Transform crossbowShotPoint;
    [SerializeField] private float timeToFullCharge = 1f;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite chargingSprite;
    [SerializeField] private Sprite chargedSprite;
    
    private float _lastAttackTime;
    private float additiveDamageBoost = 0f;

    private float holdStartTime;
    private bool isHolding;
    private bool isCharged;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GameObject.Find("ViewInHand").GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (InputHandler.Instance == null)
        {
            Debug.LogError("InputHandler.Instance is null!");
            return;
        }

        bool holdingWeapon = InputHandler.currSelectedContext == InputHandler.SelectedContext.Weapon
                              && SelfInfo.HeldItem is Weapon;

        // Crossbow charge/rotation must run every frame while held, regardless of the IsAttacking/cooldown gate
        if (holdingWeapon && ((Weapon)SelfInfo.HeldItem).AnimationType == 3)
        {
            HandleCrossbowCharging();
        }

        if (InputHandler.Instance.IsAttacking && Time.time >= _lastAttackTime + attackCooldown)
        {
            //if holding a weapon, use that instead
            if (holdingWeapon)
            {
                Weapon weapon = (Weapon)SelfInfo.HeldItem;
                switch (weapon.AnimationType)
                {
                    case 1: StartSwordAttack(); break; // swing
                    // case 2: StartStabAttack(); break;
                    case 3: StartShootAttack(); break; // shoot
                    default: StartAttack(); break;
                }
            } 
            else
            {
                StartAttack();

            }
        }
    }

    private void HandleCrossbowCharging()
    {
        // Rotate weapon to follow mouse cursor
        Vector3 difference = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float rotZ = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rotZ + crossbowOffset);

        if (Input.GetMouseButtonDown(0) && !isCharged)
        {
            isHolding = true;
            holdStartTime = Time.time;
            if (sr && idleSprite) sr.sprite = idleSprite;
        }

        if (Input.GetMouseButton(0) && isHolding)
        {
            float held = Time.time - holdStartTime;

            if (held >= timeToFullCharge)
            {
                isCharged = true;
                if (sr && chargedSprite) sr.sprite = chargedSprite;
            }
            else if (held >= timeToFullCharge * 0.5f)
            {
                if (sr && chargingSprite) sr.sprite = chargingSprite;
            }
            else
            {
                if (sr && idleSprite) sr.sprite = idleSprite;
            }
        }

        if (Input.GetMouseButtonUp(0) && isHolding)
        {
            isHolding = false;
            if (!isCharged && sr && idleSprite) sr.sprite = idleSprite;
        }
    }

    private void StartShootAttack()
    {
        if (!isCharged) return; // not ready to fire yet

        _lastAttackTime = Time.time;
        Instantiate(crossbowProjectile, crossbowShotPoint.position, transform.rotation);

        isCharged = false;
        isHolding = false;
        if (sr && idleSprite) sr.sprite = idleSprite;
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