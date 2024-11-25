using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LadderDetector : MonoBehaviour
{
    [SerializeField]
    private Playerladder player;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<ladder>())
        {
            player.ClimbingAllowed = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<ladder>())
        {
            player.ClimbingAllowed = false;
        }
    }
}