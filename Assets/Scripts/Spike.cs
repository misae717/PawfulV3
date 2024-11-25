// Spike.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spike : MonoBehaviour
{
    [Header("Reset Parameters")]
    [Tooltip("Delay before resetting the level after player touches spikes (in seconds).")]
    public float resetDelay = 1f; // Default to 1 second for visibility

    private bool hasTriggered = false; // Prevent multiple triggers

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            Debug.Log($"[{gameObject.name}] Player touched spikes! Resetting level in {resetDelay} seconds.");

            // Optionally, you can add death animations or sounds here before resetting
            Invoke(nameof(ResetLevel), resetDelay);
        }
    }

    private void ResetLevel()
    {
        Debug.Log($"[{gameObject.name}] Resetting level...");
        // Reload the current active scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize the trigger area in the Unity Editor
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
