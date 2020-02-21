using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] Character enemyPrefab;
    Stack<Character> disabledEnemies = new Stack<Character>(10);
    [SerializeField] Transform[] spawnPositions;
    [SerializeField] float spawnTimeInterval = 4;
    [SerializeField] float minSpawnTimeInterval = 1; 
    [SerializeField] float spawnRateIncreaseRate = 0.2f; 
    float spawnTime;

    public void CharacterDied(Character character)
    {
        if(character != GameManager.Instance.Player)
        {
            character.gameObject.SetActive(false);
            disabledEnemies.Push(character);
        }
    }

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        //spawn enemies in random positions
        if(spawnPositions != null && enemyPrefab != null)
        {
            if(Time.time - spawnTime > spawnTimeInterval)
            {
                int randomIndex = Random.Range(0, spawnPositions.Length);
                Spawn(spawnPositions[randomIndex].position);
                spawnTime = Time.time;
                spawnTimeInterval = Mathf.Clamp(spawnTimeInterval - spawnRateIncreaseRate, minSpawnTimeInterval, spawnTimeInterval);
            }
        }
    }

    void Spawn(Vector2 position)
    {
        Character enemy = GetEnemy();
        Character player = GameManager.Instance.Player;
        enemy.transform.position = position;
        enemy.transform.rotation = Quaternion.LookRotation(Vector3.forward, player.transform.position - (Vector3)position);

    }

    Character GetEnemy()
    {
        Character enemy;
        if (disabledEnemies.Count > 0)
        {
            enemy = disabledEnemies.Pop();
            enemy.gameObject.SetActive(true);
            enemy.Restart();
        }
        else
        {
            enemy = Instantiate(enemyPrefab);
        }
        return enemy;
    }
}
