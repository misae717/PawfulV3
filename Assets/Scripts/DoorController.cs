// DoorController.cs
using UnityEngine;

public class DoorController : MonoBehaviour
{
    [Header("Door Movement Parameters")]
    public float openDistance = 3f;
    public float moveSpeed = 2f;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen = false;

    [Header("Linked Pressure Plates")]
    public PressurePlate[] linkedPressurePlates;

    [Header("Key Requirements")]
    public Key[] requiredKeys;
    public float keyProximityRadius = 2f;
    public LayerMask playerLayer;

    private PlayerInventory playerInventory;

    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openDistance;
        playerInventory = FindObjectOfType<PlayerInventory>();
    }

    private void Update()
    {
        bool shouldOpen = false;

        // Check pressure plates
        foreach (var plate in linkedPressurePlates)
        {
            if (plate != null && plate.IsPressed())
            {
                shouldOpen = true;
                break;
            }
        }

        // Check key proximity
        if (!shouldOpen && requiredKeys.Length > 0)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, keyProximityRadius, playerLayer);
            foreach (var collider in colliders)
            {
                if (playerInventory != null && playerInventory.HasKeys(requiredKeys))
                {
                    shouldOpen = true;
                    break;
                }
            }
        }

        if (shouldOpen && !isOpen)
        {
            OpenDoor();
        }
        else if (!shouldOpen && isOpen)
        {
            CloseDoor();
        }

        // Smoothly move the door
        Vector3 targetPosition = isOpen ? openPosition : closedPosition;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
    }

    public void OpenDoor()
    {
        isOpen = true;
    }

    public void CloseDoor()
    {
        isOpen = false;
    }

    private void OnDrawGizmosSelected()
    {
        // Draw open position
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * openDistance);

        // Draw key proximity radius
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, keyProximityRadius);
    }
}
