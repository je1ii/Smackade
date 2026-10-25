using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class SpawnManager : NetworkBehaviour
{
    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private TopDownCameraFollow cameraFollow;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        cameraFollow = GetComponent<TopDownCameraFollow>();
    }

    public override void OnNetworkSpawn()
    {
        // Run on every peer: only this player's owner should process input.
        if (playerInput != null)
            playerInput.enabled = IsOwner;
        
        // Assign main camera to owner
        if (IsOwner && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<TopDownCameraFollow>();

            if (cameraFollow != null)
                cameraFollow.SetTarget(transform);
            else
                Debug.LogWarning("TopDownCameraFollow wasn't found on the main camera.");
        }
        
        // Only the server assigns spawn positions.
        if (!IsServer)
            return;

        GameObject[] objects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        Transform[] spawnPoints = objects
            .OrderBy(obj => obj.name)
            .Select(obj => obj.transform)
            .ToArray();

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning("No scene objects tagged 'SpawnPoint' were found.");
            return;
        }

        int index = (int)(OwnerClientId % (ulong)spawnPoints.Length);
        rb.position = spawnPoints[index].position;

        Debug.Log($"Client {OwnerClientId} assigned spawn point {spawnPoints[index].name}.");
    }
}
