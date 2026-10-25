using Unity.Netcode;
using UnityEngine;

public class ArmAnimation : NetworkBehaviour
{
    [SerializeField] private AnimatorOverrideController[] armSkinControllers;
    [SerializeField] private GameObject armVisualsContainer;

    private readonly NetworkVariable<int> characterIndex = new();

    private Animator animator;

    // Animator Parameter Hashes matching your Animator Controller
    private static readonly int DidFakeHash = Animator.StringToHash("didFake");
    private static readonly int WasDodgeHash = Animator.StringToHash("wasDodge");
    private static readonly int IsFailHash = Animator.StringToHash("isFail");
    private static readonly int IsSuccessHash = Animator.StringToHash("isSuccess");

    private void Awake()
    {
        animator = armVisualsContainer.GetComponentInChildren<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        characterIndex.OnValueChanged += OnCharacterChanged;

        if (IsServer && armSkinControllers != null && armSkinControllers.Length > 0)
        {
            characterIndex.Value = (int)(OwnerClientId % (ulong)armSkinControllers.Length);
        }

        ApplyCharacter(characterIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        characterIndex.OnValueChanged -= OnCharacterChanged;
    }

    // --- Server-Authoritative Result Triggers ---

    public void PlayCommitResult(bool isSuccess)
    {
        if (!IsServer) return;
        PlayCommitResultClientRpc(isSuccess);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayCommitResultClientRpc(bool isSuccess)
    {
        if (animator == null) return;

        if (HasParameter(animator, IsSuccessHash)) animator.SetBool(IsSuccessHash, isSuccess);
        if (HasParameter(animator, IsFailHash)) animator.SetBool(IsFailHash, !isSuccess);
    }

    public void PlayFakeAction()
    {
        if (!IsServer) return;
        PlayFakeClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayFakeClientRpc()
    {
        if (animator != null && HasParameter(animator, DidFakeHash))
        {
            animator.SetTrigger(DidFakeHash);
        }
    }

    public void PlayDodgeAction(bool wasDodge)
    {
        if (!IsServer) return;
        PlayDodgeClientRpc(wasDodge);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayDodgeClientRpc(bool wasDodge)
    {
        if (animator != null && HasParameter(animator, WasDodgeHash))
        {
            animator.SetBool(WasDodgeHash, wasDodge);
        }
    }

    public void ResetAnimatorParameters()
    {
        if (!IsServer) return;
        ResetParametersClientRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetParametersClientRpc()
    {
        if (animator == null) return;

        if (HasParameter(animator, WasDodgeHash)) animator.SetBool(WasDodgeHash, false);
        if (HasParameter(animator, IsFailHash)) animator.SetBool(IsFailHash, false);
        if (HasParameter(animator, IsSuccessHash)) animator.SetBool(IsSuccessHash, false);
    }

    // Helper to check if an animator controller actually contains a parameter hash
    private bool HasParameter(Animator anim, int hash)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.nameHash == hash) return true;
        }
        return false;
    }

    // --- State Sync Handlers ---

    private void OnCharacterChanged(int oldValue, int newValue) => ApplyCharacter(newValue);

    private void ApplyCharacter(int index)
    {
        if (animator == null || armSkinControllers == null || armSkinControllers.Length == 0)
            return;

        int safeIndex = ((index % armSkinControllers.Length) + armSkinControllers.Length) % armSkinControllers.Length;
        animator.runtimeAnimatorController = armSkinControllers[safeIndex];
    }
}