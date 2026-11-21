using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;

    [Header("Upgrade de movimento")]
    [SerializeField, Min(1f)] private float upgradedMoveSpeedMultiplier = 1.25f;

    public float CurrentMoveSpeed => moveSpeed * (UpgradePurchases.Has(UpgradePurchases.Shoes)
        ? Mathf.Max(1f, upgradedMoveSpeedMultiplier) : 1f) *
        (TimedPotionEffects.Instance != null ? TimedPotionEffects.Instance.GetMultiplier(TimedPotionKind.MovementSpeed) : 1f);

    private PlayerControls playerControls;
    private Vector2 movement;
    private Vector2 lastMovement;
    public Vector2 MoveDirection => movement;
    public Vector2 FacingDirection => lastMovement.normalized;
    private PlayerCombat combat;

    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;

    private void Awake()
    {
        playerControls = new PlayerControls();

        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<PlayerCombat>();
        // Render between physics ticks; Cinemachine follows this interpolated pose in LateUpdate.
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        myAnimator = GetComponent<Animator>();
        mySpriteRender = GetComponent<SpriteRenderer>();

        // Começa olhando para baixo
        lastMovement = Vector2.down;
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Update()
    {
        PlayerInput();
        if (combat != null && combat.OwnsMotion) return;
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void PlayerInput()
    {
        var keys = Keyboard.current;
        if (InventoryPanelSession.HasOpenPanel && keys != null &&
            (keys.upArrowKey.isPressed || keys.downArrowKey.isPressed || keys.leftArrowKey.isPressed || keys.rightArrowKey.isPressed))
        {
            movement = Vector2.zero;
            return;
        }
        movement = playerControls.Movement.Move.ReadValue<Vector2>();
    }

    private void Move()
    {
        if (combat != null && combat.OwnsMotion) return;
        rb.MovePosition(
            rb.position + movement * (CurrentMoveSpeed * Time.fixedDeltaTime)
        );
    }

    private void UpdateAnimation()
    {
        // Só atualiza a última direção quando o personagem estiver andando
        if (movement != Vector2.zero)
        {
            lastMovement = movement;
            mySpriteRender.flipX = movement.x < 0f;
        }

        // Envia a direção para o Animator
        myAnimator.SetFloat("moveX", lastMovement.x);
        myAnimator.SetFloat("moveY", lastMovement.y);

        // Indica se o personagem está andando ou parado
        myAnimator.SetFloat("speed", movement.magnitude);
    }
}
