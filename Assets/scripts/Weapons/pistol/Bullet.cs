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
        rigidbody = GetComponent<Rigidbody2D>();
    }

    public void SetSpeed(Vector2 direction)        //control flying by direction and speed
    {
        rigidbody.linearVelocity = direction * speed;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null)
        {
            character.TakeDamage(damage);
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Wall"))
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public void SetDamage(float _damage)
    {
        damage=_damage;
    }
}