using UnityEngine;

public class pistol : MonoBehaviour
{
    public float interval;
    public GameObject bulletPrefab;
    public GameObject shellPrefab;
    public AudioClip shootSound; // 新增的音效变量
    private Transform muzzlePos;
    private Transform shellPos;
    private Vector2 mousePos;
    private Vector2 direction;
    private float timer = 0;
    private float flipY;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        muzzlePos = transform.Find("Muzzle");
        shellPos = transform.Find("BulletShell");
        flipY = transform.localScale.y;
    }

    void Update()
    {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (mousePos.x < transform.position.x)
        {
            transform.localScale = new Vector3(flipY, -flipY, 1);
        }
        else
        {
            transform.localScale = new Vector3(flipY, flipY, 1);
        }
        Shoot();
    }

    void Shoot()
    {
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive)
            return;
        direction = (mousePos - new Vector2(transform.position.x, transform.position.y)).normalized;
        transform.right = direction;
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Fire1") && timer <= 0)
        {
            Fire();
            timer = interval;
        }
    }

    void Fire()
    {
        animator.SetTrigger("Shoot");
        GameObject bullet = Instantiate(bulletPrefab, muzzlePos.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetSpeed(direction);

        // 新增音效播放代码
        if (shootSound != null)
        {
            AudioSource.PlayClipAtPoint(shootSound, muzzlePos.position);
        }
    }
}