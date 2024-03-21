using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DAIMBEnemySpawner : MonoBehaviour
{
    public GameObject prefab;
    public int spawnRate;
    public int limit;
    public bool instantSpawn;
    public int instantSpawnCount;

    private int current;
    private int count;
    private bool instantSpawned;
    private bool done;

    // Start is called before the first frame update
    void Start()
    {
        current = 0;
        count = 0;
        instantSpawned = false;
        done = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (instantSpawn && !instantSpawned)
        {
            for (int i = 0; i < instantSpawnCount; ++i)
            {
                const float range = 5.0f;
                float randomX = Random.Range(-range, range);
                float randomY = Random.Range(-range, range);
                Vector3 position = new(randomX, randomY, 0.0f);
                Quaternion rotation = prefab.transform.rotation;
                Instantiate(prefab, position, rotation);
            }
            instantSpawned = true;
            done = true;
            count = instantSpawnCount;
        }
        else if (!done)
        {
            ++current;
            if (current == spawnRate)
            {
                if (count == limit)
                    return;
                Instantiate(prefab);
                current = 0;
                ++count;
            }
        }
    }
}
