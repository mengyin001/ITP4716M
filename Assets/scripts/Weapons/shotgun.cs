using UnityEngine;

public class shotgun : gun
{
    [System.Serializable]
    public class SpreadSettings
    {
        public int pelletCount = 5;
        [Range(0, 45)] public float angle = 15f;
        public float damagePerPellet = 5f;
    }

    [Header("霰弹枪设置")] 
    public SpreadSettings spreadSettings;
    public ParticleSystem muzzleFlash;

    protected override void Start()
    {
        base.Start();
        
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
            var main = muzzleFlash.main;
            main.playOnAwake = false;
        }
    }

    protected override void Fire()
    {
        base.Fire(); // 调用基类射击逻辑
        
        if (muzzleFlash != null)
            muzzleFlash.Play();
        
        if (bulletPrefab == null || muzzlePos == null) 
        {
            Debug.LogError("霰弹枪无法射击 - 缺少必要组件");
            return;
        }

        int median = spreadSettings.pelletCount / 2;
        for (int i = 0; i < spreadSettings.pelletCount; i++)
        {
            float angle = spreadSettings.pelletCount % 2 == 1 ? 
                spreadSettings.angle * (i - median) : 
                spreadSettings.angle * (i - median) + spreadSettings.angle * 0.5f;
            
            FirePellet(angle);
        }
    }

    private void FirePellet(float angle)
    {
        Vector2 pelletDir = Quaternion.AngleAxis(angle, Vector3.forward) * direction;
        
        GameObject pellet = Instantiate(bulletPrefab, muzzlePos.position, Quaternion.identity);
        Bullet bullet = pellet.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.SetSpeed(pelletDir);
            bullet.SetDamage(spreadSettings.damagePerPellet);
        }
        else
        {
            Debug.LogWarning("霰弹枪子弹缺少Bullet组件", pellet);
        }
    }
}