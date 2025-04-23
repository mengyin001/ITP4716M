using UnityEngine;

public class LaserBullet : Bullet
{
    public float maxDistance = 100f;
    private Vector2 startPosition;
    private bool isDestroyed = false;

    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;
    }

    void Update()
    {
        // Destroy bullet if it travels too far
        if (Vector2.Distance(startPosition, transform.position) >= maxDistance && !isDestroyed)
        {
            DestroyBullet();
        }
    }

    private void DestroyBullet()
    {
        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        isDestroyed = true;
        Destroy(gameObject);
    }

    public bool IsDestroyed()
    {
        return isDestroyed;
    }

    protected override void OnTriggerEnter2D(Collider2D other)
    {
        // Only collide with walls
        if (other.CompareTag("Wall"))
        {
            DestroyBullet();
        }
        // Pass through enemies without stopping
    }
}