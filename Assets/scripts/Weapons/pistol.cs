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
    private float timer=0;
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
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        direction = (mousePos - new Vector2(transform.position.x, transform.position.y)).normalized;
        direction = (mousePos-new Vector2(transform.position.x, transform.position.y)).normalized;      //detect mouse direction
        transform.right=direction;
        if (timer > 0)
        {
            timer -= Time.deltaTime; // 減少計時器
        }

        // 檢查開火輸入和計時器
        if (Input.GetButtonDown("Fire1") && timer <= 0)
        {
            Fire();
            timer = interval; // 重置計時器
        }
    }
    
    void Fire(){
        animator.SetTrigger("Shoot");
        GameObject bullet=Instantiate(bulletPrefab,muzzlePos.position,Quaternion.identity);      //shoot
        bullet.GetComponent<Bullet>().SetSpeed(direction);
        Instantiate(shellPrefab,shellPos.position,shellPos.rotation);
    }
}
