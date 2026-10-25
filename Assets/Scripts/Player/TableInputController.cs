using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TableInputController : NetworkBehaviour
{
    [SerializeField] private ArmAnimation armAnimation;

    private void Awake()
    {
        if (armAnimation == null)
        {
            armAnimation = GetComponent<ArmAnimation>();
        }
    }

    public void OnAction(InputValue value)
    {
        if (!IsOwner || !enabled) return;

        if (value.isPressed)
        {
            // Dynamically check current role per action
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.IsLocalPlayerSlapper())
            {
                CommitActionServerRpc();
            }
            else
            {
                DodgeActionServerRpc();
            }
        }
    }

    public void OnFake(InputValue value)
    {
        if (!IsOwner || !enabled) return;

        if (value.isPressed)
        {
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.IsLocalPlayerSlapper())
            {
                FakeActionServerRpc();
            }
        }
    }

    // =========================================================================
    // SERVER-SIDE RPCs
    // =========================================================================

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CommitActionServerRpc()
    {
        if (GameNetworkManager.Instance == null) return;

        bool isP1 = OwnerClientId == 0;
        bool p1IsSlapper = (GameNetworkManager.Instance.ActiveRole.Value == PlayerRole.Player1IsSlapper);

        // Security check: Verify the sender is actually authorized to commit a slap
        if ((isP1 && !p1IsSlapper) || (!isP1 && p1IsSlapper)) return;
        
        GameNetworkManager.Instance.ExecuteSlapDecisionServerRpc(SlapType.Commit);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void DodgeActionServerRpc()
    {
        if (GameNetworkManager.Instance == null) return;
        GameNetworkManager.Instance.PerformDodgeServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void FakeActionServerRpc()
    {
        if (GameNetworkManager.Instance == null) return;
        GameNetworkManager.Instance.ExecuteSlapDecisionServerRpc(SlapType.Fake);
    }
}