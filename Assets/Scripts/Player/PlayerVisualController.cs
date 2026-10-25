using Unity.Netcode;
using UnityEngine;

public class PlayerVisualController : NetworkBehaviour
{
    [Header("Visual Containers")]
    [SerializeField] private GameObject characterVisuals;
    [SerializeField] private GameObject armVisuals;   

    [Header("Scripts to Toggle")]
    [SerializeField] private PlayerMovement playerMovement; 
    
    private readonly NetworkVariable<bool> isAtTable = new(false);

    public override void OnNetworkSpawn()
    {
        isAtTable.OnValueChanged += OnTableStateChanged;
    
        ApplyVisualState(isAtTable.Value);
    }

    public override void OnNetworkDespawn()
    {
        isAtTable.OnValueChanged -= OnTableStateChanged;
    }

    public void SetAtTableState(bool atTable)
    {
        if (!IsServer) return;
        isAtTable.Value = atTable;
    }

    private void OnTableStateChanged(bool oldValue, bool newValue)
    {
        ApplyVisualState(newValue);
    }

    private void ApplyVisualState(bool atTable)
    {
        if (characterVisuals != null) characterVisuals.SetActive(!atTable);
        if (armVisuals != null) armVisuals.SetActive(atTable);

        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(!atTable);
        }
    }
}