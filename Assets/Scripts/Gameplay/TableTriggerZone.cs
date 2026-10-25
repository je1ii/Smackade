using Unity.Netcode;
using UnityEngine;

public class TableTriggerZone : NetworkBehaviour
{
    [Header("Table Camera & Desk Positions")]
    [SerializeField] private Transform singleTableAnchor;
    [SerializeField] private Transform p1DeskPosition;
    [SerializeField] private Transform p2DeskPosition;

    [Header("Child Colliders")]
    [SerializeField] private Collider2D p1TriggerCollider;
    [SerializeField] private Collider2D p2TriggerCollider;

    private bool isP1Ready = false;
    private bool isP2Ready = false;
    private bool hasGameStarted = false; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;
        if (hasGameStarted) return;

        if (other.TryGetComponent<NetworkObject>(out var playerNetworkObject))
        {
            ulong clientId = playerNetworkObject.OwnerClientId;

            if (clientId == 0 && other.IsTouching(p1TriggerCollider)) isP1Ready = true;
            else if (clientId == 1 && other.IsTouching(p2TriggerCollider)) isP2Ready = true;

            if (isP1Ready && isP2Ready && GameNetworkManager.Instance.CurrentPhase.Value == GamePhase.CoinToss)
            {
                hasGameStarted = true; 
                
                TransitionToTableGamePlay();
                
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.InitializeHUD();
                }

                GameNetworkManager.Instance.InitiateCoinTossServerRpc();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.TryGetComponent<NetworkObject>(out var playerNetworkObject))
        {
            ulong clientId = playerNetworkObject.OwnerClientId;

            if (clientId == 0 && !other.IsTouching(p1TriggerCollider)) isP1Ready = false;
            else if (clientId == 1 && !other.IsTouching(p2TriggerCollider)) isP2Ready = false;
        }
    }

    private void TransitionToTableGamePlay()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                ulong clientId = client.ClientId;
                Transform deskTransform = (clientId == 0) ? p1DeskPosition : p2DeskPosition;
                
                client.PlayerObject.transform.position = deskTransform.position;
                client.PlayerObject.transform.rotation = deskTransform.rotation;

                if (client.PlayerObject.TryGetComponent<PlayerVisualController>(out var visualController))
                {
                    visualController.SetAtTableState(true);
                }
            }
        }

        SetupClientCamerasAndUI_Rpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetupClientCamerasAndUI_Rpc()
    {
        if (Camera.main != null && Camera.main.TryGetComponent<TopDownCameraFollow>(out var cameraFollow))
        {
            cameraFollow.SnapToEnvironment(singleTableAnchor);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.InitializeHUD();
        }
    }
}