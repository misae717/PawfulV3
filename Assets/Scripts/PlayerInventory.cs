// PlayerInventory.cs
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private List<Key> keys = new List<Key>();

    public void AddKey(Key key)
    {
        if (!keys.Contains(key))
        {
            keys.Add(key);
            Debug.Log("Key collected: " + key.keyID);
        }
    }

    public bool HasKeys(Key[] requiredKeys)
    {
        foreach (var requiredKey in requiredKeys)
        {
            bool hasKey = keys.Exists(k => k.keyID == requiredKey.keyID);
            if (!hasKey)
                return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        // Optional: Visualize the keys the player has
    }
}
