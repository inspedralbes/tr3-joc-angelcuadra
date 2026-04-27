using UnityEngine;

public class CoinSpawner : MonoBehaviour
{
    public GameObject coinPrefab;
    public Vector2 spawnRange = new Vector2(4f, 4f); 

    public void SpawnCoin()
    {
        float x = Random.Range(-spawnRange.x, spawnRange.x);
        float y = Random.Range(-spawnRange.y, spawnRange.y);
        
        Instantiate(coinPrefab, new Vector3(x, y, 0), Quaternion.identity);
    }
}
