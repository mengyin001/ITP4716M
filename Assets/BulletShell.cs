using System.Collections;
using UnityEngine;

public class BulletShell : MonoBehaviour
{
    public float speed;
    public float stopTime = .5f;
    public float fadeSpeed = .01f;
    private new Rigidbody2D rigidbody;
    private SpriteRenderer sprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        rigidbody.linearVelocity=Vector3.up * speed;
        StartCoroutine(Stop());
    }

    // Update is called once per frame
    IEnumerator Stop(){
        yield return new WaitForSeconds(stopTime);
        rigidbody.gravityScale = 0;

        while(sprite.color.a>0){
            sprite.color=new Color(sprite.color.r, sprite.color.g, sprite.color.b, sprite.color.a - fadeSpeed);  //change color
            yield return new WaitForFixedUpdate();
        }
        Destroy(gameObject);
    }
}
