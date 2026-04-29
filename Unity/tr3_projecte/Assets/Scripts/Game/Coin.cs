using UnityEngine;

public class Coin : MonoBehaviour
{
    private void Update()
    {
        // Una petita rotació perquè es vegi més "viva"
        transform.Rotate(Vector3.forward * 90 * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Avisem al GameManager que hem recollit una moneda
            GameManager.Instance.CoinCollected();
            Destroy(gameObject);
        }
    }
}
