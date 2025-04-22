// 金币拾取脚本（附加在金币预制体上）
using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [Header("配置")]
    public int value = 1;
    public ParticleSystem pickupEffect;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PickupCoin();
        }
    }

    void PickupCoin()
    {
        // 更新货币
        MoneyManager.Instance.AddMoney(value);

        // 播放效果
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        // 销毁金币
        Destroy(gameObject);
    }
}