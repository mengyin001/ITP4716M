using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed;
    public GameObject explosionPrefab;
    new private Rigidbody2D rigidbody;
    public float damage = 5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()        //awake much fast
    {
        rigidbody= GetComponent<Rigidbody2D>();
    }
    
    public void SetSpeed(Vector2 direction){        //control flying by direction and speed
        rigidbody.linearVelocity = direction * speed;
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other){              //collide with explosion prefab and destroy itself
        if (other.CompareTag("Enemy")) // Check if the collided object has the "Enemy" tag
        {
            Enemy Enemy = other.GetComponent<Enemy>();
            if (Enemy != null)
            {
                Enemy.TakeDamage(damage); // Adjust the damage value as needed
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);             
            }
            else if (other.CompareTag("Wall")) // Add other collision checks
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }   
    }
}
