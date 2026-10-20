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

    [Header("Sword/Spear Settings")]
    [SerializeField] private float damageMult = 1.5f;
    
    [Header("Crossbow Settings")]
    [SerializeField] private float shotSpawnDistance = 0.6f;
    [SerializeField] private GameObject crossbowProjectile;
    [SerializeField] private Transform crossbowShotPoint;
    [SerializeField] private float timeToFullCharge = 1f;
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite chargingSprite;
    [SerializeField] private Sprite chargedSprite;

    [Header("Aiming")]
    [SerializeField] private float attackPointDistance = 0.75f;
    [SerializeField] private float defaultArc = 120f;           // total arc in degrees
    [SerializeField] private float swordArc = 120f;
    [SerializeField] private float spearArc = 40f;
    private Vector2 aimDirection = Vector2.right;

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
                damageMult = weapon.DamageMult;
                switch (weapon.AnimationType)
                {
                    case 1: StartSwordAttack(); break; // swing
                    case 2: StartStabAttack(); break;
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
        if (Input.GetMouseButtonDown(1) && !isCharged)
        {
            isHolding = true;
            holdStartTime = Time.time;
            if (sr && idleSprite) sr.sprite = idleSprite;
        }

        if (Input.GetMouseButton(1) && isHolding && !isCharged)
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

        if (Input.GetMouseButtonUp(1) && isHolding && !isCharged)
        {
            isHolding = false;
            if (sr && idleSprite) sr.sprite = idleSprite;
        }
    }

    private void StartSwordAttack()
    {
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTime = Time.time;
        animator.SetTrigger("Sword");
        Invoke("DetectSwordHits", 0.5f);
    }

    private void StartStabAttack()
    {
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTime = Time.time;
        animator.SetTrigger("Spear");
        Invoke("DetectSpearHits", 0.5f);
    }

    private void StartShootAttack()
    {
        if (!isCharged) return; // not ready to fire yet

        _lastAttackTime = Time.time;
        
        Vector2 dir = GetDirectionToMouse(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector2 spawnPos = (Vector2)crossbowShotPoint.position + dir * shotSpawnDistance;

        Instantiate(crossbowProjectile, spawnPos, Quaternion.Euler(0f, 0f, angle - 90f));

        isCharged = false;
        isHolding = false;
        if (sr && idleSprite) sr.sprite = idleSprite;
    }

    private void StartAttack()
    {
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
        Invoke("DetectHits", 0.5f);
    }

    public void EndAttack() 
    {

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

        int swordDamage = Mathf.RoundToInt(attackDamage * damageMult);
        ApplyHits(hitEnemies, swordDamage);
    }

    private void DetectSpearHits()
    {
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange + 2
        );

        int spearDamage = Mathf.RoundToInt(attackDamage * damageMult);
        ApplyHits(hitEnemies, spearDamage);
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
                if (IsBlockedByWall(enemy))
                {
                    continue;
                }

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
                //Debug.Log(knockbackVector);
            }     
        }
        SelfInfo.isAnimated = false;
    }

    private bool IsBlockedByWall(Collider2D target)
    {
        Vector2 origin = transform.position;
        Vector2 targetPoint = target.ClosestPoint(origin);

        RaycastHit2D[] hits = Physics2D.LinecastAll(origin, targetPoint);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.collider.CompareTag("PlaceTileMap")) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    private Vector2 GetDirectionToMouse(Vector2 from)
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorld - from;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : aimDirection;
    }
}