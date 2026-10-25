using UnityEngine;

public class TopDownCameraFollow : MonoBehaviour
{
    [SerializeField, Min(0f)] private float followSpeed = 10f;
    [SerializeField] private Vector2 offset;

    [Header("Axis Constraints")]
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private float fixedYPosition = 0f;

    private Transform target;
    private bool isAnchored = false;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        isAnchored = false;
    }

    public void SnapToEnvironment(Transform singleTableAnchor)
    {
        isAnchored = true;
        target = null; // Detach player tracking

        Vector3 anchorPos = new Vector3(singleTableAnchor.position.x, singleTableAnchor.position.y, transform.position.z);
        transform.position = anchorPos;
    }

    private void LateUpdate()
    {
        // Locked at the table anchor position during gameplay
        if (isAnchored || target == null) return;

        // Lobby Follow Mode: Horizontal X-axis tracking
        Vector2 targetPosition = (Vector2)target.position + offset;
        Vector2 currentPosition = transform.position;

        float blend = 1f - Mathf.Exp(-followSpeed * Time.deltaTime);
        float smoothedX = Mathf.Lerp(currentPosition.x, targetPosition.x, blend);
        float targetY = lockYAxis ? fixedYPosition : Mathf.Lerp(currentPosition.y, targetPosition.y, blend);

        transform.position = new Vector3(smoothedX, targetY, transform.position.z);
    }
}