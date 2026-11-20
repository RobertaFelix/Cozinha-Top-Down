using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
public sealed class EnemyCombat : MonoBehaviour
{
    public enum EnemyState { Idle, Chase, Windup, Recovery, Hurt, Dead }
    [Header("Attributes")]
    [SerializeField, Min(1)] private int maximumHealth = 50;
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(0)] private float moveSpeed = 1.1f;
    [SerializeField, Min(.1f)] private float detectionRange = 5;
    [SerializeField, Min(.1f)] private float leashRange = 8;
    [SerializeField, Min(.1f)] private float attackRange = .9f;
    [SerializeField, Min(.05f)] private float windupSeconds = .5f;
    [SerializeField, Min(.05f)] private float recoverySeconds = .8f;
    [SerializeField, Min(.01f)] private float hurtSeconds = .25f;
    [SerializeField, Min(0)] private float knockbackSpeed = 4.5f;
    [SerializeField, Min(.01f)] private float damageFlashSeconds = .14f;
    [Header("Art")]
    [SerializeField] private Sprite[] idleFrames, walkFrames, attackFrames, hurtFrames, deathFrames;
    [SerializeField, Min(.01f)] private float frameSeconds = .1f;
    public int CurrentHealth { get; private set; }
    public EnemyState State { get; private set; }
    private Rigidbody2D body;
    private CircleCollider2D feet;
    private SpriteRenderer actor;
    private PlayerHealth target;
    private Vector2 home, attackDirection, knockback;
    private float timer, animationTime;
    private float lastDamageTime = float.NegativeInfinity;
    private bool struck;
    private readonly RaycastHit2D[] hits = new RaycastHit2D[24];

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>(); feet = GetComponent<CircleCollider2D>(); actor = GetComponent<SpriteRenderer>();
        home = body.position;
        CurrentHealth = maximumHealth;
    }
    private void Start() { target = FindFirstObjectByType<PlayerHealth>(); }
    private bool HasTarget => target && target.InDungeon && target.Current > 0 && target.gameObject.activeInHierarchy;
    private void Update()
    {
        if (Time.timeScale <= 0) return;
        animationTime += Time.deltaTime;
        if (State == EnemyState.Dead)
        {
            Animate(deathFrames, false);
            if (animationTime >= Mathf.Max(.4f, (deathFrames?.Length ?? 0) * frameSeconds)) Destroy(gameObject);
            return;
        }
        timer += Time.deltaTime;
        if (State == EnemyState.Hurt)
        {
            Animate(hurtFrames, false);
            if (timer >= hurtSeconds) ChangeState(EnemyState.Recovery);
            return;
        }
        if (State == EnemyState.Windup)
        {
            actor.color = new Color(1, .65f, .55f);
            Animate(attackFrames, false);
            if (timer >= windupSeconds)
            {
                if (!struck && HasTarget)
                {
                    var combat = target.GetComponent<PlayerCombat>();
                    Vector2 point = combat && combat.Hurtbox ? combat.Hurtbox.ClosestPoint(body.position) : (Vector2)target.transform.position;
                    Vector2 delta = point - body.position;
                    if (delta.magnitude <= attackRange && Vector2.Angle(attackDirection, delta) <= 70 && HasLineOfSight(delta))
                        target.TakeDamage(damage);
                }
                struck = true;
                ChangeState(EnemyState.Recovery);
            }
            return;
        }
        actor.color = Color.white;
        if (State == EnemyState.Recovery)
        {
            Animate(idleFrames);
            if (timer >= recoverySeconds) ChangeState(EnemyState.Idle);
            return;
        }
        if (!HasTarget) { ChangeState(EnemyState.Idle); Animate(idleFrames); return; }
        Vector2 toPlayer = (Vector2)target.transform.position - body.position;
        bool aggro = toPlayer.magnitude <= detectionRange && Vector2.Distance(home, target.transform.position) <= leashRange && HasLineOfSight(toPlayer);
        if (!aggro) { ChangeState(EnemyState.Idle); Animate(idleFrames); return; }
        actor.flipX = toPlayer.x < 0;
        if (toPlayer.magnitude <= attackRange)
        {
            attackDirection = toPlayer.normalized;
            struck = false;
            ChangeState(EnemyState.Windup);
        }
        else { ChangeState(EnemyState.Chase); Animate(walkFrames); }
    }
    private void FixedUpdate()
    {
        if (State == EnemyState.Chase && HasTarget)
            Move(((Vector2)target.transform.position - body.position).normalized * moveSpeed * Time.fixedDeltaTime);
        else if (State == EnemyState.Hurt) Move(knockback * knockbackSpeed * Time.fixedDeltaTime);
    }
    private void LateUpdate()
    {
        float sinceHit = Time.time - lastDamageTime;
        if (sinceHit < damageFlashSeconds)
            actor.color = (int)(sinceHit / .045f) % 2 == 0 ? new Color(1, .12f, .12f) : Color.white;
        else actor.color = State == EnemyState.Windup ? new Color(1, .65f, .55f) : Color.white;
    }
    private bool HasLineOfSight(Vector2 delta)
    {
        int count = Physics2D.Raycast(body.position, delta.normalized, new ContactFilter2D { useTriggers = false }, hits, delta.magnitude);
        for (int i = 0; i < count; i++)
        {
            var other = hits[i].collider;
            if (other && other.attachedRigidbody != body && !other.GetComponentInParent<EnemyCombat>() && !other.GetComponentInParent<PlayerHealth>()) return false;
        }
        return true;
    }
    private void Move(Vector2 step)
    {
        float distance = step.magnitude;
        if (distance <= 0) return;
        int count = feet.Cast(step.normalized, new ContactFilter2D { useTriggers = false }, hits, distance + .02f);
        for (int i = 0; i < count; i++)
            if (hits[i].collider && hits[i].collider.attachedRigidbody != body)
                distance = Mathf.Min(distance, Mathf.Max(0, hits[i].distance - .02f));
        body.MovePosition(body.position + step.normalized * distance);
    }
    public bool TakeDamage(int amount, Vector2 direction)
    {
        if (State == EnemyState.Dead || amount <= 0) return false;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        knockback = direction.normalized;
        lastDamageTime = Time.time;
        ChangeState(CurrentHealth == 0 ? EnemyState.Dead : EnemyState.Hurt);
        timer = animationTime = 0;
        actor.color = Color.white;
        if (State == EnemyState.Dead) { feet.enabled = false; body.linearVelocity = Vector2.zero; }
        return true;
    }
    private void ChangeState(EnemyState next)
    {
        if (State == next) return;
        State = next; timer = 0; animationTime = 0;
    }
    private void Animate(Sprite[] frames, bool loop = true)
    {
        if (frames == null || frames.Length == 0) return;
        int index = (int)(animationTime / frameSeconds);
        actor.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
