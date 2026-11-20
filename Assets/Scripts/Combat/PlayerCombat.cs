using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerController), typeof(PlayerHealth), typeof(PlayerStamina))]
public sealed class PlayerCombat : MonoBehaviour
{
    public enum ActionState { Ready, Attack, Roll, Hurt, Dead }
    [Header("Sword")]
    [SerializeField, Min(1)] private int swordDamage = 25;
    [SerializeField, Min(.1f)] private float swordReach = 1.25f;
    [SerializeField, Range(10, 180)] private float swordArc = 120;
    [SerializeField, Min(.01f)] private float attackSeconds = .55f;
    [SerializeField, Range(0, 1)] private float attackActiveStart = .32f;
    [SerializeField, Range(0, 1)] private float attackActiveEnd = .65f;
    [Header("Roll")]
    [SerializeField, Min(0)] private float rollCost = 25;
    [SerializeField, Min(.05f)] private float rollSeconds = .3f;
    [SerializeField, Min(0)] private float rollSpeed = 9;
    [SerializeField, Range(0, 1)] private float rollInvulnerableFraction = .5f;
    [Header("Hit response")]
    [SerializeField, Min(.01f)] private float hurtSeconds = .18f;
    [SerializeField, Min(0)] private float hitInvulnerabilitySeconds = .6f;
    [SerializeField, Min(.1f)] private float deathSeconds = .8f;
    [Header("Animation frames (feet-aligned sprites)")]
    [SerializeField] private Sprite[] attackFrames, rollFrames, hurtFrames, deathFrames;
    [SerializeField] private HotbarController hotbar;
    [Header("Damage area (independent from the feet collider)")]
    [SerializeField] private Collider2D hurtbox;
    public Collider2D Hurtbox => hurtbox ? hurtbox : feet;
    public ActionState State { get; private set; }
    public bool OwnsMotion => State != ActionState.Ready;
    public bool IsInvulnerable => State == ActionState.Dead || Time.time < invulnerableUntil ||
        (State == ActionState.Roll && elapsed < rollSeconds * rollInvulnerableFraction);
    public Vector2 ActionDirection => direction;
    private PlayerController movement;
    private PlayerHealth health;
    private PlayerStamina stamina;
    private Animator animator;
    private SpriteRenderer actor;
    private Rigidbody2D body;
    private Collider2D feet;
    private SceneTransitionController transitions;
    private float elapsed, duration, invulnerableUntil;
    private Vector2 direction = Vector2.down;
    private bool restoreAnimator;
    private Sprite restingSprite;
    private bool restingFlip;
    private readonly HashSet<EnemyCombat> struck = new HashSet<EnemyCombat>();
    private readonly RaycastHit2D[] walls = new RaycastHit2D[24];
    private readonly Collider2D[] targets = new Collider2D[32];

    private void Awake()
    {
        movement = GetComponent<PlayerController>(); health = GetComponent<PlayerHealth>();
        stamina = GetComponent<PlayerStamina>(); animator = GetComponent<Animator>();
        actor = GetComponent<SpriteRenderer>(); body = GetComponent<Rigidbody2D>();
        feet = GetComponent<Collider2D>();
    }
    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        SceneManager.activeSceneChanged += OnSceneChanged;
    }
    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        SceneManager.activeSceneChanged -= OnSceneChanged;
        FinishAction();
    }
    private void OnSceneChanged(Scene oldScene, Scene next)
    {
        FinishAction(); invulnerableUntil = 0;
    }
    private bool CanAct() => isActiveAndEnabled && health.InDungeon && health.Current > 0 && movement.enabled &&
        State == ActionState.Ready && !CombatInputGate.IsBlocked() &&
        (!transitions || !transitions.IsLoading);

    private void Update()
    {
        if (!transitions) transitions = FindFirstObjectByType<SceneTransitionController>();
        if (State == ActionState.Ready)
        {
            if (!CanAct()) return;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                TryRoll(movement.MoveDirection.sqrMagnitude > .01f ? movement.MoveDirection : movement.FacingDirection);
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
                !CombatInputGate.PointerOverUI(Mouse.current.position.ReadValue()))
            {
                var camera = Camera.main;
                Vector2 aim = camera ? (Vector2)camera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) - body.position : movement.FacingDirection;
                TryAttack(aim);
            }
            return;
        }
        if (Time.timeScale <= 0) return;
        float previousElapsed = elapsed;
        elapsed += Time.deltaTime;
        if (State == ActionState.Attack && elapsed >= duration * attackActiveStart && previousElapsed <= duration * attackActiveEnd)
            HitTargets();
        DrawFrame();
        if (elapsed < duration) return;
        if (State == ActionState.Dead)
        {
            if (transitions) transitions.TryReturnHome();
        }
        else FinishAction();
    }
    private void FixedUpdate()
    {
        if (State != ActionState.Roll || !movement.enabled || CombatInputGate.IsBlocked()) return;
        float distance = rollSpeed * Time.fixedDeltaTime;
        var filter = new ContactFilter2D { useTriggers = false };
        int count = feet.Cast(direction, filter, walls, distance + .025f);
        for (int i = 0; i < count; i++)
            if (walls[i].collider && walls[i].collider.attachedRigidbody != body && Vector2.Dot(direction, walls[i].normal) < -.001f)
                distance = Mathf.Min(distance, Mathf.Max(0, walls[i].distance - .025f));
        body.MovePosition(body.position + direction * distance);
    }
    public bool TryAttack(Vector2 aim)
    {
        if (!CanAct()) return false;
        if (!hotbar) hotbar = FindFirstObjectByType<HotbarController>();
        var item = hotbar ? hotbar.EquippedSlot?.inventoryItem?.itemPrefab : null;
        if (!item || !item.GetComponent<SwordItem>()) return false;
        Begin(ActionState.Attack, aim, attackSeconds);
        struck.Clear();
        return true;
    }
    public bool TryRoll(Vector2 aim)
    {
        if (!CanAct() || !stamina.TrySpend(rollCost)) return false;
        Begin(ActionState.Roll, aim, rollSeconds);
        return true;
    }
    private void Begin(ActionState state, Vector2 aim, float seconds)
    {
        if (State == ActionState.Ready)
        {
            restoreAnimator = animator && animator.enabled;
            restingSprite = actor.sprite;
            restingFlip = actor.flipX;
        }
        State = state; duration = seconds; elapsed = 0;
        if (state == ActionState.Dead)
        {
            InventoryStackCursor.Cancel();
            ItemDragHandler.CancelAllDrags();
            InventoryPanelSession.CloseCurrent();
        }
        direction = aim.sqrMagnitude > .001f ? aim.normalized : movement.FacingDirection;
        if (animator) animator.enabled = false;
        body.linearVelocity = Vector2.zero;
        DrawFrame();
    }
    private void DrawFrame()
    {
        var frames = State == ActionState.Attack ? attackFrames : State == ActionState.Roll ? rollFrames :
            State == ActionState.Hurt ? hurtFrames : deathFrames;
        if (frames != null && frames.Length > 0)
            actor.sprite = frames[Mathf.Min(frames.Length - 1, (int)(elapsed / duration * frames.Length))];
        // Current artwork faces right. Vertical attacks keep a lateral pose until new art arrives.
        if (Mathf.Abs(direction.x) > .05f) actor.flipX = direction.x < 0;
    }
    private void HitTargets()
    {
        int count = Physics2D.OverlapCircle(body.position, swordReach, new ContactFilter2D { useTriggers = false }, targets);
        for (int i = 0; i < count; i++)
        {
            var enemy = targets[i].GetComponentInParent<EnemyCombat>();
            if (!enemy || struck.Contains(enemy)) continue;
            Vector2 delta = (Vector2)enemy.transform.position - body.position;
            if (Vector2.Angle(direction, delta) > swordArc * .5f) continue;
            // A wall between the attacker and target blocks the sword.
            bool occluded = false;
            int hits = Physics2D.Raycast(body.position, delta.normalized, new ContactFilter2D { useTriggers = false }, walls, delta.magnitude);
            for (int j = 0; j < hits; j++)
            {
                var collider = walls[j].collider;
                if (collider && collider.attachedRigidbody != body && !collider.GetComponentInParent<EnemyCombat>()) { occluded = true; break; }
            }
            if (occluded) continue;
            struck.Add(enemy);
            enemy.TakeDamage(swordDamage, direction);
        }
    }
    private void OnDamaged(int amount)
    {
        invulnerableUntil = Time.time + hitInvulnerabilitySeconds;
        Begin(health.Current <= 0 ? ActionState.Dead : ActionState.Hurt, -direction,
            health.Current <= 0 ? deathSeconds : hurtSeconds);
    }
    private void FinishAction()
    {
        if (State == ActionState.Ready) return;
        State = ActionState.Ready;
        if (actor) { actor.sprite = restingSprite; actor.flipX = restingFlip; }
        if (animator) animator.enabled = restoreAnimator;
        if (body) body.linearVelocity = Vector2.zero;
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, swordReach);
        Gizmos.DrawRay(transform.position, direction * swordReach);
    }
}
