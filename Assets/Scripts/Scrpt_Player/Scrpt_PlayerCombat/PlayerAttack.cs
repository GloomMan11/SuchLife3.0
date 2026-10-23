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
    private float holdStartTime;
    private bool isHolding;
    private bool isCharged;

    [Header("Aiming")]
    [SerializeField] private float attackPointDistance = 0.75f;
    [SerializeField] private float defaultArc = 120f;           // total arc in degrees
    [SerializeField] private float swordArc = 120f;
    [SerializeField] private float spearArc = 40f;
    private Vector2 aimDirection = Vector2.right;

    [Header("Detecting hits")]
    private HashSet<Collider2D> alreadyHit = new HashSet<Collider2D>(); 
    private bool DetectingHits;
    private Collider2D[] hitObjects = new Collider2D[10];


    private float _lastAttackTime;
    private float additiveDamageBoost = 0f;
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
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTime = Time.time;
        animator.SetTrigger("Sword");
        StartCoroutine(DetectSwordHits());
    }

    private void StartStabAttack()
    {
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTime = Time.time;
        animator.SetTrigger("Spear");
        StartCoroutine(DetectSpearHits());
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
        if (DetectingHits) 
        {
            return;
        }
        aimDirection = GetDirectionToMouse(transform.position);
        SelfInfo.isAnimated = true;
        _lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
        StartCoroutine(DetectHits());
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

    private IEnumerator DetectHits()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPoint.position;
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

    private IEnumerator DetectSwordHits()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;
        int swordDamage = Mathf.RoundToInt(attackDamage * damageMult);

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPoint.position;
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

    private IEnumerator DetectSpearHits()
    {
        DetectingHits = true;
        alreadyHit.Clear();
        float elapsed = 0f;
        int spearDamage = Mathf.RoundToInt(attackDamage * damageMult);

        while (elapsed < 1.0)
        {
            Vector2 currentPos = attackPoint.position;
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