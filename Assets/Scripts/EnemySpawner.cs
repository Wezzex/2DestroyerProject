using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float spawnRate, spawnRateIncrease;
    [SerializeField] private GameObject spawnPosition;
    [SerializeField] private GameObject[] enemyPrefab;

    [SerializeField] private float spawnRangeMax, spawnRangedMin; 


    private bool canSpawn = true;


    private void Start()
    {
        StartCoroutine(Spawner());
    }

    private IEnumerator Spawner()
    {
        WaitForSeconds wait = new WaitForSeconds(spawnRate);

        while (true)
        {
            yield return wait;

            int rand = Random.Range(0, enemyPrefab.Length);
            GameObject enemyToSpawn = enemyPrefab[rand];

            Instantiate(enemyToSpawn, new Vector2(Random.Range(spawnRangedMin, spawnRangeMax), Random.Range(spawnRangedMin, spawnRangeMax)), Quaternion.identity);

        }
    }

    
}
