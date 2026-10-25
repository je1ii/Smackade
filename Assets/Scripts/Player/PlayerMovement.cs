using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float speed = 5f;

    private Rigidbody2D rb;
    private PlayerAnimation characterAnimation;
    private Vector2 moveInput;
    private bool canMove = true;

    public void setCanMove(bool value) => canMove = value;
    

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        characterAnimation = GetComponent<PlayerAnimation>();
    }

    public void OnMove(InputValue value)
    {
        if (!IsOwner || !canMove)
            return;

        SendMoveInputRpc(Vector2.ClampMagnitude(value.Get<Vector2>(), 1f));
    }

    public void OnPause(InputValue value)
    {
        if (!value.isPressed)
            return;

        UIManager.Instance.TogglePauseMenu();
    }

    [Rpc(SendTo.Server)]
    private void SendMoveInputRpc(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);
    }

    private void FixedUpdate()
    {
        if (!IsServer)
            return;

        rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * moveInput);
        characterAnimation?.SetMovementVisuals(moveInput);
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        enabled = isEnabled;

        if (!isEnabled)
        {
            // Stop all physical momentum immediately when disabled!
            if (TryGetComponent<Rigidbody2D>(out var rb2d))
            {
                rb2d.linearVelocity = Vector2.zero;
                rb2d.angularVelocity = 0f;
            }
            else if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
