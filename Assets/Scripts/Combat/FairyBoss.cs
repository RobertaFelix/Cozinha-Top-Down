using System.Collections;
using UnityEngine;

public sealed class FairyBoss : SwordTarget
{
    public int Health { get; private set; }
    private FairyArena arena;
    private DungeonExpeditionStyle style;
    private SpriteRenderer actor;
    private PlayerHealth player;
    private Rigidbody2D body;
    private Sprite[] frames;
    private float animationStart, flashUntil, cooldown = 1.4f;
    private bool casting;
    private enum Spell { FallingStar, Cone, Nova, Spiral }
    private static readonly Spell[] Opening = { Spell.FallingStar, Spell.Cone };
    private static readonly Spell[] EnragedCycle = { Spell.Nova, Spell.Spiral, Spell.FallingStar, Spell.Cone };
    private int phaseOneAttack, phaseTwoAttack;
    public int Maximum => style.fairyHealth;
    private readonly RaycastHit2D[] boltHits = new RaycastHit2D[12];
    private bool Enraged => Health * 2 <= style.fairyHealth;
    private bool Fighting => player && player.InDungeon && player.Current > 0 &&
        arena.Contains(player.transform.position);

    public void Initialize(FairyArena owner, DungeonExpeditionStyle settings)
    {
        arena = owner; style = settings; Health = style.fairyHealth;
        actor = GetComponent<SpriteRenderer>(); actor.sortingOrder = 6;
        body = GetComponent<Rigidbody2D>();
        player = FindFirstObjectByType<PlayerHealth>();
        SetPose(style.fairyIdle);
    }
    public void RestoreHealth(int value) => Health = Mathf.Clamp(value, 1, style.fairyHealth);
    private void SetPose(Sprite[] pose)
    {
        frames = pose;
        animationStart = Time.time;
        actor.sprite = frames[0];
    }
    private void Update()
    {
        actor.sprite = frames[(int)((Time.time - animationStart) * 9) % frames.Length];
        actor.color = Time.time < flashUntil ? new Color(1, .32f, .28f) :
            Enraged ? new Color(1, .76f, .86f) : Color.white;
        if (!Fighting)
        {
            if (casting || transform.childCount > 0)
            {
                StopAllCoroutines();
                foreach (Transform child in transform) Destroy(child.gameObject);
                casting = false; cooldown = 1.4f; SetPose(style.fairyIdle);
            }
            return;
        }
        actor.flipX = player.transform.position.x < transform.position.x;
        if (casting) return;
        cooldown -= Time.deltaTime;
        if (cooldown <= 0) StartCoroutine(Attack());
    }
    private void FixedUpdate()
    {
        if (!Fighting || casting) return;
        Vector2 home = arena.transform.position;
        Vector2 destination = (Vector2)player.transform.position +
            new Vector2(Mathf.Cos(Time.time * .7f) * 2.2f, Mathf.Sin(Time.time * .7f) * 1.5f);
        var offset = destination - home;
        destination = home + new Vector2(Mathf.Clamp(offset.x, -4, 4), Mathf.Clamp(offset.y, -2.5f, 2.5f));
        body.MovePosition(Vector2.MoveTowards(body.position, destination, Time.fixedDeltaTime * (Enraged ? 2 : 1.4f)));
    }
    private IEnumerator Attack()
    {
        casting = true;
        SetPose(style.fairyCharge);
        Spell pattern = Enraged ? EnragedCycle[phaseTwoAttack++ % EnragedCycle.Length] : Opening[phaseOneAttack++ % Opening.Length];
        switch (pattern)
        {
            case Spell.FallingStar: yield return FallingStar(player.transform.position); break;
            case Spell.Cone: yield return Cone(); break;
            case Spell.Nova: yield return Nova(); break;
            case Spell.Spiral: yield return Spiral(); break;
        }
        SetPose(style.fairyIdle);
        cooldown = Enraged ? .85f : 1.5f;
        casting = false;
    }
    private SpriteRenderer Effect(string name, Sprite frame, Vector2 position)
    {
        var effect = new GameObject(name).AddComponent<SpriteRenderer>();
        effect.transform.SetParent(transform, true); effect.transform.position = position;
        effect.sortingOrder = 7; effect.sprite = frame;
        return effect;
    }
    private IEnumerator FallingStar(Vector2 impact)
    {
        const float radius = .9f, warning = .82f;
        var effect = Effect("Falling star", style.meteor[0], impact);
        var ring = effect.gameObject.AddComponent<LineRenderer>();
        ring.useWorldSpace = true; ring.loop = true; ring.positionCount = 24;
        ring.sharedMaterial = effect.sharedMaterial; ring.sortingOrder = 6;
        ring.startWidth = ring.endWidth = .07f;
        ring.startColor = ring.endColor = new Color(1, .64f, .18f);
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i * Mathf.PI * 2 / ring.positionCount;
            ring.SetPosition(i, impact + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
        }
        for (float t = 0; t < warning; t += Time.deltaTime)
        {
            effect.transform.position = impact + Vector2.up * (3.5f * (1 - t / warning));
            effect.sprite = style.meteor[(int)(t * 12) % style.meteor.Length];
            yield return null;
        }
        SetPose(style.fairyCast);
        ring.enabled = false; effect.transform.position = impact;
        var hurtbox = player.GetComponent<PlayerCombat>()?.Hurtbox;
        Vector2 point = hurtbox ? hurtbox.ClosestPoint(impact) : (Vector2)player.transform.position;
        if (Vector2.Distance(point, impact) <= radius) player.TakeDamage(style.fireDamage);
        foreach (var frame in style.fire) { effect.sprite = frame; yield return new WaitForSeconds(.05f); }
        Destroy(effect.gameObject);
    }
    private LineRenderer Warning(string label, int points, Color color)
    {
        var marker = new GameObject(label).AddComponent<LineRenderer>();
        marker.transform.SetParent(transform,false);
        marker.useWorldSpace = true;
        marker.loop = true;
        marker.positionCount = points;
        marker.sharedMaterial = actor.sharedMaterial;
        marker.sortingOrder = 9;
        marker.startWidth = marker.endWidth = .09f;
        marker.startColor = marker.endColor = color;
        return marker;
    }

    private static Vector2 Unit(float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(a),Mathf.Sin(a));
    }
    private Vector2 ClosestPlayerPoint(Vector2 origin)
    {
        var hurtbox = player.GetComponent<PlayerCombat>()?.Hurtbox;
        return hurtbox ? hurtbox.ClosestPoint(origin) : (Vector2)player.transform.position;
    }
    private IEnumerator Cone()
    {
        const float halfAngle = 32f, reach = 4.5f;
        Vector2 origin = body.position;
        Vector2 direction = ((Vector2)player.transform.position - origin).normalized;
        float angle = Mathf.Atan2(direction.y,direction.x) * Mathf.Rad2Deg;
        var warning = Warning("Frost cone warning",18,new Color(.54f,.87f,1));
        warning.SetPosition(0,origin);
        for (int i = 1; i < warning.positionCount; i++)
            warning.SetPosition(i,origin + Unit(angle - halfAngle + 2 * halfAngle * (i - 1) / 16) * reach);
        yield return new WaitForSeconds(.85f);
        SetPose(style.fairyCast);
        Vector2 offset = ClosestPlayerPoint(origin) - origin;
        if (offset.magnitude <= reach && Vector2.Angle(direction,offset) <= halfAngle) player.TakeDamage(style.iceDamage);
        for (int i = -2; i <= 2; i++) StartCoroutine(Flash(style.ice,origin + Unit(angle + i * halfAngle / 2) * 2.6f));
        yield return new WaitForSeconds(.18f);
        Destroy(warning.gameObject);
    }
    private IEnumerator Nova()
    {
        const float reach = 4f, duration = 1.55f;
        Vector2 origin = body.position;
        var ring = Warning("Expanding nova",48,new Color(1,.49f,.24f));
        var limit = Warning("Nova blast radius",48,new Color(1,.69f,.37f,.6f));
        for (int i = 0; i < 48; i++) limit.SetPosition(i,origin + Unit(i * 7.5f) * reach);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            for (int i = 0; i < 48; i++) ring.SetPosition(i,origin + Unit(i * 7.5f) * (reach * t / duration));
            yield return null;
        }
        SetPose(style.fairyCast);
        if ((ClosestPlayerPoint(origin) - origin).magnitude <= reach) player.TakeDamage(style.fireDamage);
        StartCoroutine(Flash(style.fire, origin));
        for (int i = 0; i < 16; i++) StartCoroutine(Flash(style.fire,origin + Unit(i * 22.5f) * reach));
        Destroy(ring.gameObject); Destroy(limit.gameObject);
        yield return new WaitForSeconds(.6f);
    }
    private IEnumerator Flash(Sprite[] sprites, Vector2 position)
    {
        var fx = Effect("Spell impact",sprites[0],position);
        foreach (var sprite in sprites)
        {
            fx.transform.position = position;
            fx.sprite = sprite;
            yield return new WaitForSeconds(.04f);
        }
        Destroy(fx.gameObject);
    }
    private IEnumerator Spiral()
    {
        yield return new WaitForSeconds(.55f);
        SetPose(style.fairyCast);
        for (int angle = 0; angle < 360; angle += 15)
        {
            for (int direction = 0; direction < 4; direction++)
                StartCoroutine(StarBolt(Unit(angle + direction * 90)));
            yield return new WaitForSeconds(.14f);
        }
    }
    private IEnumerator StarBolt(Vector2 direction)
    {
        Vector2 position = body.position;
        var effect = Effect("Spiral star",style.meteor[0],position);
        var hurtbox = player.GetComponent<PlayerCombat>()?.Hurtbox;
        for (float elapsed = 0; Fighting; elapsed += Time.deltaTime)
        {
            Vector2 step = direction * (Time.deltaTime * 4f);
            int count = Physics2D.CircleCast(position,.14f,direction,
                new ContactFilter2D { useTriggers = true },boltHits,step.magnitude);
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                var hit = boltHits[i].collider;
                if (!hit || hit.attachedRigidbody == body) continue;
                if (hit == hurtbox || (!hurtbox && hit.GetComponentInParent<PlayerHealth>() == player))
                { player.TakeDamage(style.iceDamage); blocked = true; break; }
                if (!hit.isTrigger && !hit.GetComponentInParent<PlayerHealth>())
                { blocked = true; break; }
            }
            position += step;
            if (blocked || !arena.Contains(position)) break;
            effect.transform.position = position;
            effect.sprite = style.meteor[(int)(elapsed * 10) % style.meteor.Length];
            yield return null;
        }
        Destroy(effect.gameObject);
    }
    public override bool TakeDamage(int amount, Vector2 direction)
    {
        if (amount <= 0 || Health == 0) return false;
        Health = Mathf.Max(0, Health - amount);
        flashUntil = Time.time + .15f;
        if (Health == 0) { arena.Defeat(); gameObject.SetActive(false); Destroy(gameObject); }
        return true;
    }
}
