using UnityEngine;

public class MoneyPickup : MonoBehaviour
{
    [SerializeField] private int value = 10; // 单个金钱的价值
    [SerializeField] private AudioClip pickupSound; // 捡起音效

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            MoneyManager.Instance.AddMoney(value);

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);

            Destroy(gameObject);
        }
    }
}