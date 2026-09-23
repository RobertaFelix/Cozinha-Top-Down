using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1f;

    private PlayerControls playerControls;
    private Vector2 movement;
    private Vector2 lastMovement;

    private Rigidbody2D rb;
    private Animator myAnimator;
    private SpriteRenderer mySpriteRender;

    private void Awake()
    {
        playerControls = new PlayerControls();

        rb = GetComponent<Rigidbody2D>();
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
        UpdateAnimation();
        AdjustPlayerFacingDirection();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void PlayerInput()
    {
        movement = playerControls.Movement.Move.ReadValue<Vector2>();
    }

    private void Move()
    {
        rb.MovePosition(
            rb.position + movement * (moveSpeed * Time.fixedDeltaTime)
        );
    }

    private void UpdateAnimation()
    {
        // Só atualiza a última direção quando o personagem estiver andando
        if (movement != Vector2.zero)
        {
            lastMovement = movement;
        }

        // Envia a direção para o Animator
        myAnimator.SetFloat("moveX", lastMovement.x);
        myAnimator.SetFloat("moveY", lastMovement.y);

        // Indica se o personagem está andando ou parado
        myAnimator.SetFloat("speed", movement.magnitude);
    }

    private void AdjustPlayerFacingDirection()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();

        Vector3 playerScreenPoint =
            Camera.main.WorldToScreenPoint(transform.position);

        // Só usamos flipX para a animação lateral
        if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
        {
            if (mousePos.x < playerScreenPoint.x)
            {
                mySpriteRender.flipX = true;
            }
            else
            {
                mySpriteRender.flipX = false;
            }
        }
    }
}