// PressurePlate.cs (For 2D Games)
using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Header("Linked Doors")]
    public DoorController[] linkedDoors;

    [Header("Sinking Parameters")]
    public float sinkDistance = 0.5f; // Increased for better visibility
    public float sinkSpeed = 5f; // Increased for faster sinking
    private Vector3 originalPosition;
    private Vector3 targetPosition;
    private bool isPressed = false;

    [Header("Detection Parameters")]
    public float detectionRadius = 1f; // Increased radius
    public LayerMask detectionLayers;

    private void Start()
    {
        originalPosition = transform.position;
        targetPosition = originalPosition - Vector3.up * sinkDistance;
    }

    private void Update()
    {
        bool currentlyPressed = IsPressed();

        if (currentlyPressed && !isPressed)
        {
            OnPressed();
        }
        else if (!currentlyPressed && isPressed)
        {
            OnReleased();
        }

        isPressed = currentlyPressed;

        // Smoothly move the pressure plate
        Vector3 desiredPosition = isPressed ? targetPosition : originalPosition;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * sinkSpeed);

        // Debugging: Log the current position
        // Uncomment the line below to see position updates in the Console
        // Debug.Log($"PressurePlate '{gameObject.name}' Position: {transform.position}");
    }

    // Changed to Physics2D for 2D games
    public bool IsPressed()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, detectionLayers);
        Debug.Log($"PressurePlate '{gameObject.name}' detected {colliders.Length} colliders.");
        return colliders.Length > 0;
    }

    private void OnPressed()
    {
        foreach (var door in linkedDoors)
        {
            door.OpenDoor();
        }
    }

    private void OnReleased()
    {
        foreach (var door in linkedDoors)
        {
            door.CloseDoor();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = isPressed ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * sinkDistance);
        }
        else
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * sinkDistance);
        }
    }
}
