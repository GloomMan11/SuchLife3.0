using Unity.VisualScripting;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("References")]
    [SerializeField] private Transform attackPointLeft;
    [SerializeField] private Transform attackPointRight;
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
    private float holdStartTime;
    private bool isHolding;
    private bool isCharged;

    [Header("Aiming")]
    // [SerializeField] private float attackPointDistance = 0.75f;
    // [SerializeField] private float defaultArc = 120f;           // total arc in degrees
    // [SerializeField] private float swordArc = 120f;
    // [SerializeField] private float spearArc = 40f;
    private Vector2 aimDirection = Vector2.right;

    [Header("Detecting hits")]
    private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>(); 
    private bool DetectingHits;
    private Collider2D[] hitObjects = new Collider2D[10];


    private float _lastAttackTimeLeft;
    private float _lastAttackTimeRight;
    private float additiveDamageBoost = 0f;
    private SpriteRenderer srLeft;
    private SpriteRenderer srRight;

    private void Awake()
    {
        srLeft = GameObject.Find("ViewInHandLeft").GetComponent<SpriteRenderer>();
        srRight = GameObject.Find("ViewInHandRight").GetComponent<SpriteRenderer>();
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

        if (InputHandler.Instance.IsAttackingLeft && Time.time >= _lastAttackTimeLeft + attackCooldown)
        {
            //if holding a weapon, use that instead
            if (holdingWeapon)
            {
                Weapon weapon = (Weapon)SelfInfo.HeldItem;
                damageMult = weapon.DamageMult;
                switch (weapon.AnimationType)
                {
                    case 1: StartSwordAttackLeft(); break; // swing
                    case 2: StartStabAttackLeft(); break;
                    case 3: StartShootAttack(); break; // shoot
                    default: StartAttackLeft(); break;
                }
            } 
            else
            {
                StartAttackLeft();
            }
        }

        if (InputHandler.Instance.IsAttackingRight && Time.time >= _lastAttackTimeRight + attackCooldown)
        {
            if (holdingWeapon)
            {
                Weapon weapon = (Weapon)SelfInfo.HeldItem;
                damageMult = weapon.DamageMult;
                switch (weapon.AnimationType)
                {
                    case 1: StartSwordAttackRight(); break; // swing
                    case 2: StartStabAttackRight(); break;
                    case 3: StartShootAttack(); break; // shoot
                    default: StartAttackRight(); break;
                }
            } 
            else
            {
                StartAttackRight();
            }
        }
    }

    private void HandleCrossbowCharging()
    {
        if (Input.GetMouseButtonDown(1) && !isCharged)
        {
            isHolding = true;
            holdStartTime = Time.time;
            if (srLeft && idleSprite) srLeft.sprite = idleSprite;
            if (srRight && idleSprite) srRight.sprite = idleSprite;
        }

        if (Input.GetMouseButton(1) && isHolding && !isCharged)
        {
            float held = Time.time - holdStartTime;

            if (held >= timeToFullCharge)
            {
                isCharged = true;
                if (srLeft && chargedSprite) srLeft.sprite = chargedSprite;
                if (srRight && chargedSprite) srRight.sprite = chargedSprite;
            }
            else if (held >= timeToFullCharge * 0.5f)
            {
                if (srLeft && chargingSprite) srLeft.sprite = chargingSprite;
                if (srRight && chargingSprite) srRight.sprite = chargingSprite;
            }
            else
            {
                if (srLeft && idleSprite) srLeft.sprite = idleSprite;
                if (srRight && idleSprite) srRight.sprite = idleSprite;
            }
        }

        if (Input.GetMouseButtonUp(1) && isHolding && !isCharged)
        {
            isHolding = false;
            if (srLeft && idleSprite) srLeft.sprite = idleSprite;
            if (srRight && idleSprite) srRight.sprite = idleSprite;
        }
    }

    private void StartSwordAttackLeft()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTimeLeft = Time.time;
        animator.SetTrigger("SwordLeft");
        StartCoroutine(DetectSwordHitsLeft());
    }

    private void StartSwordAttackRight()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTimeRight = Time.time;
        animator.SetTrigger("SwordRight");
        StartCoroutine(DetectSwordHitsRight());
    }

    private void StartStabAttackLeft()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTimeLeft = Time.time;
        animator.SetTrigger("SpearLeft");
        StartCoroutine(DetectSpearHitsLeft());
    }

    private void StartStabAttackRight()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTimeRight = Time.time;
        animator.SetTrigger("SpearRight");
        StartCoroutine(DetectSpearHitsRight());
    }

    private void StartShootAttack()
    {
        if (!isCharged) return; // not ready to fire yet

        _lastAttackTimeLeft = Time.time;
        
        Vector2 dir = GetDirectionToMouse(transform.position);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Vector2 spawnPos = (Vector2)crossbowShotPoint.position + dir * shotSpawnDistance;

        Instantiate(crossbowProjectile, spawnPos, Quaternion.Euler(0f, 0f, angle - 90f));

        isCharged = false;
        isHolding = false;
        if (srLeft && idleSprite) srLeft.sprite = idleSprite;
        if (srRight && idleSprite) srRight.sprite = idleSprite;
    }

    private void StartAttackLeft()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTimeLeft = Time.time;
        animator.SetTrigger("AttackLeft");
        StartCoroutine(DetectHitsLeft());
    }

    private void StartAttackRight()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTimeRight = Time.time;
        animator.SetTrigger("AttackRight");
        StartCoroutine(DetectHitsRight());
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

    private IEnumerator DetectHitsLeft()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPointLeft.position;
            int count = Physics2D.OverlapCircleNonAlloc(currentPos, 0.5f, hitObjects);

            for (int i = 0; i < count; i++)
            {
                Collider2D hitObject = hitObjects[i];
                
                if (!alreadyHit.Contains(hitObject))
                {
                    alreadyHit.Add(hitObject);
                    ApplyHits(hitObject, attackDamage);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DetectingHits = false;
    }

    private IEnumerator DetectHitsRight()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPointRight.position;
            int count = Physics2D.OverlapCircleNonAlloc(currentPos, 0.5f, hitObjects);

            for (int i = 0; i < count; i++)
            {
                Collider2D hitObject = hitObjects[i];
                
                if (!alreadyHit.Contains(hitObject))
                {
                    alreadyHit.Add(hitObject);
                    ApplyHits(hitObject, attackDamage);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DetectingHits = false;
    }

    private IEnumerator DetectSwordHitsLeft()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;
        int swordDamage = Mathf.RoundToInt(attackDamage * damageMult);

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPointLeft.position;
            int count = Physics2D.OverlapCircleNonAlloc(currentPos, 0.5f, hitObjects);

            for (int i = 0; i < count; i++)
            {
                Collider2D hitObject = hitObjects[i];
                
                if (!alreadyHit.Contains(hitObject))
                {
                    alreadyHit.Add(hitObject);
                    ApplyHits(hitObject, swordDamage);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DetectingHits = false;
    }

    private IEnumerator DetectSwordHitsRight()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;
        int swordDamage = Mathf.RoundToInt(attackDamage * damageMult);

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPointRight.position;
            int count = Physics2D.OverlapCircleNonAlloc(currentPos, 0.5f, hitObjects);

            for (int i = 0; i < count; i++)
            {
                Collider2D hitObject = hitObjects[i];
                
                if (!alreadyHit.Contains(hitObject))
                {
                    alreadyHit.Add(hitObject);
                    ApplyHits(hitObject, swordDamage);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DetectingHits = false;
    }

    private IEnumerator DetectSpearHitsLeft()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;
        int spearDamage = Mathf.RoundToInt(attackDamage * damageMult);

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPointLeft.position;
            int count = Physics2D.OverlapCircleNonAlloc(currentPos, 0.5f, hitObjects);

            for (int i = 0; i < count; i++)
            {
                Collider2D hitObject = hitObjects[i];
                
                if (!alreadyHit.Contains(hitObject))
                {
                    alreadyHit.Add(hitObject);
                    ApplyHits(hitObject, spearDamage);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DetectingHits = false;
    }

    private IEnumerator DetectSpearHitsRight()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;
        int spearDamage = Mathf.RoundToInt(attackDamage * damageMult);

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPointRight.position;
            int count = Physics2D.OverlapCircleNonAlloc(currentPos, 0.5f, hitObjects);

            for (int i = 0; i < count; i++)
            {
                Collider2D hitObject = hitObjects[i];
                
                if (!alreadyHit.Contains(hitObject))
                {
                    alreadyHit.Add(hitObject);
                    ApplyHits(hitObject, spearDamage);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        DetectingHits = false;
    }

    private void ApplyHits(Collider2D enemy, int Damage)
    {
        if (enemy.TryGetComponent<Mob>(out var mob))
        {
            if (IsBlockedByWall(enemy) == true)
            {
                return;
            }

            if (enemy.TryGetComponent<Health>(out var health))
            {
                int boostedDamage = Mathf.RoundToInt(Damage + additiveDamageBoost);
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
        if (attackPointLeft == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    private Vector2 GetDirectionToMouse(Vector2 from)
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = mouseWorld - from;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : aimDirection;
    }
}