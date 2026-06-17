using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public string          itemName;       // just for Inspector label
        public GameObject      itemPrefab;     // the pickup prefab
        public NavigationNode[] possibleNodes; // where it CAN spawn
    }

    [Header("Items to Spawn at Game Start")]
    public List<SpawnEntry> spawnEntries;

    [Header("Spawn Height")]
    [Tooltip("How high above the node floor the item appears")]
    public float spawnHeight = 0.8f;

    private List<GameObject> _spawnedItems = new List<GameObject>();

    private void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        // Clear any previously spawned items
        foreach (var item in _spawnedItems)
            if (item != null) Destroy(item);
        _spawnedItems.Clear();

        foreach (var entry in spawnEntries)
        {
            if (entry.itemPrefab == null)
            {
                Debug.LogWarning($"[Spawner] {entry.itemName} has no prefab assigned!");
                continue;
            }

            if (entry.possibleNodes == null || entry.possibleNodes.Length == 0)
            {
                Debug.LogWarning($"[Spawner] {entry.itemName} has no spawn nodes!");
                continue;
            }

            // Pick a random node from the possible list
            int idx  = Random.Range(0, entry.possibleNodes.Length);
            var node = entry.possibleNodes[idx];

            if (node == null) continue;

            // Spawn item as child of that node
            var spawnPos = node.transform.position + Vector3.up * spawnHeight;
            var item     = Instantiate(entry.itemPrefab, spawnPos, Quaternion.identity, node.transform);

            _spawnedItems.Add(item);
            Debug.Log($"[Spawner] {entry.itemName} spawned at {node.nodeName}");
        }
    }
}