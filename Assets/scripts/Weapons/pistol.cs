using UnityEngine;

public class pistol : MonoBehaviour
{
    public float interval;
    public GameObject bulletPrefab;
    public GameObject shellPrefab;
    private Transform muzzlePos;
    private Transform shellPos;
    private Vector2 mousePos;
    private Vector2 direction;
    private float timer;
    private float flipY;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();
        muzzlePos = transform.Find("Muzzle");
        shellPos = transform.Find("BulletShell");
        flipY=transform.localScale.y;
    }

    // Update is called once per frame
    void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);     //flip
        if(mousePos.x<transform.position.x){
            transform.localScale =new Vector3(flipY, -flipY,1);
        }
        else{
            transform.localScale =new Vector3(flipY, flipY,1);
        }
        Shoot();
    }

    void Shoot(){
        direction = (mousePos-new Vector2(transform.position.x, transform.position.y)).normalized;      //detect mouse direction
        transform.right=direction;
        if(timer!=0){
            timer -= Time.deltaTime;        //gun cooldown time
            if (timer<=0){
                timer = 0;
            }
        }
        if(Input.GetButton("Fire1")){
            if (timer == 0){
                Fire();
                timer = interval;
            }
        }
    }
    
    void Fire(){
        animator.SetTrigger("Shoot");
        GameObject bullet=Instantiate(bulletPrefab,muzzlePos.position,Quaternion.identity);      //shoot
        bullet.GetComponent<Bullet>().SetSpeed(direction);

        Instantiate(shellPrefab,shellPos.position,shellPos.rotation);
    }
}
