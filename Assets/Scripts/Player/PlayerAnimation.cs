using Unity.Netcode;
using UnityEngine;

public class PlayerAnimation : NetworkBehaviour
{
    [SerializeField] private AnimatorOverrideController[] characterControllers;
    [SerializeField] private GameObject characterVisualContainer;

    private readonly NetworkVariable<int> characterIndex = new();
    private readonly NetworkVariable<bool> isWalking = new();
    private readonly NetworkVariable<bool> facingLeft = new();

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private static readonly int IsWalkingHash =
        Animator.StringToHash("isWalking");

    private void Awake()
    {
        // Restrict to roaming visuals
        animator = characterVisualContainer.GetComponentInChildren<Animator>();
        spriteRenderer = characterVisualContainer.GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        characterIndex.OnValueChanged += OnCharacterChanged;
        isWalking.OnValueChanged += OnWalkingChanged;
        facingLeft.OnValueChanged += OnFacingChanged;

        if (IsServer && characterControllers != null &&
            characterControllers.Length > 0)
        {
            characterIndex.Value =
                (int)(OwnerClientId % (ulong)characterControllers.Length);
        }

        ApplyCharacter(characterIndex.Value);
        ApplyWalking(isWalking.Value);
        ApplyFacing(facingLeft.Value);
    }

    public override void OnNetworkDespawn()
    {
        characterIndex.OnValueChanged -= OnCharacterChanged;
        isWalking.OnValueChanged -= OnWalkingChanged;
        facingLeft.OnValueChanged -= OnFacingChanged;
    }

    // Call this on the server.
    public void SetMovementVisuals(Vector2 input)
    {
        if (!IsServer)
            return;

        isWalking.Value = input.sqrMagnitude > 0.01f;

        if (input.x > 0.01f)
            facingLeft.Value = false;
        else if (input.x < -0.01f)
            facingLeft.Value = true;
    }

    private void OnCharacterChanged(int oldValue, int newValue) =>
        ApplyCharacter(newValue);

    private void OnWalkingChanged(bool oldValue, bool newValue) =>
        ApplyWalking(newValue);

    private void OnFacingChanged(bool oldValue, bool newValue) =>
        ApplyFacing(newValue);

    private void ApplyWalking(bool walking)
    {
        if (animator != null)
            animator.SetBool(IsWalkingHash, walking);
    }

    private void ApplyFacing(bool left)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = left;
    }

    private void ApplyCharacter(int index)
    {
        if (animator == null || characterControllers == null ||
            characterControllers.Length == 0)
            return;

        int safeIndex = ((index % characterControllers.Length)
                         + characterControllers.Length)
                        % characterControllers.Length;

        animator.runtimeAnimatorController = characterControllers[safeIndex];
    }
}
