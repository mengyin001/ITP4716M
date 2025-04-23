using UnityEngine;

public class LaserGun : gun
{
    [Header("Laser Settings")]
    public LineRenderer laserBeam;
    public float laserWidth = 0.1f;
    public Color laserColor = Color.green;
    public float laserBulletSpeed = 50f;
    public float maxLaserDistance = 100f;
    public float damagePerSecond = 10f;
    
    [Header("Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem beamParticles; // Continuous beam effect
    
    private LaserBullet currentLaserBullet;
    private bool isFiring = false;
    private Vector2 laserEndPoint;
    private bool initialFire = false;

    protected override void Awake()
    {
        base.Awake();
        InitializeLaserBeam();
        
        if (muzzleFlash != null)
        {
            muzzleFlash.Stop();
            var main = muzzleFlash.main;
            main.playOnAwake = false;
        }
        
        if (beamParticles != null)
        {
            beamParticles.Stop();
            var beamMain = beamParticles.main;
            beamMain.playOnAwake = false;
        }
    }

    private void InitializeLaserBeam()
    {
        if (laserBeam == null)
        {
            laserBeam = gameObject.AddComponent<LineRenderer>();
        }
        
        laserBeam.startWidth = laserWidth;
        laserBeam.endWidth = laserWidth;
        laserBeam.material = new Material(Shader.Find("Sprites/Default"));
        laserBeam.startColor = laserColor;
        laserBeam.endColor = laserColor;
        laserBeam.positionCount = 2;
        laserBeam.enabled = false;
    }

    protected override void Update()
    {
        if (Camera.main == null) return;
        
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        UpdateWeaponRotation();
        
        if (playerMovement != null && !playerMovement.isOpen)
        {
            HandleLaserFire();
        }
        
        if (isFiring)
        {
            UpdateLaserBeam();
        }
    }

    private void HandleLaserFire()
    {
        if ((healthSystem != null && healthSystem.IsDead) || 
            (DialogueSystem.Instance != null && DialogueSystem.Instance.isDialogueActive))
        {
            StopFiring();
            return;
        }
            
        if (Input.GetButton("Fire1"))
        {
            if (!isFiring && (healthSystem == null || healthSystem.HasEnoughEnergy(energyCostPerShot)))
            {
                initialFire = true;
                StartFiring();
            }
            else if (isFiring && healthSystem != null && !healthSystem.HasEnoughEnergy(energyCostPerShot * Time.deltaTime))
            {
                StopFiring();
            }
        }
        else if (isFiring)
        {
            StopFiring();
        }
        
        // Continuous energy drain while firing
        if (isFiring && healthSystem != null)
        {
            healthSystem.ConsumeEnergy(energyCostPerShot * Time.deltaTime / interval);
        }
    }

    private void StartFiring()
    {
        isFiring = true;
        
        // Only play initial fire effects once
        if (initialFire)
        {
            Fire();
            initialFire = false;
        }
        
        laserBeam.enabled = true;
        
        if (muzzleFlash != null && initialFire)
            muzzleFlash.Play();
            
        if (beamParticles != null)
            beamParticles.Play();
    }

    private void StopFiring()
    {
        isFiring = false;
        initialFire = false;
        if (currentLaserBullet != null)
        {
            Destroy(currentLaserBullet.gameObject);
            currentLaserBullet = null;
        }
        laserBeam.enabled = false;
        
        if (muzzleFlash != null)
            muzzleFlash.Stop();
            
        if (beamParticles != null)
            beamParticles.Stop();
    }

    protected override void Fire()
    {
        if (bulletPrefab == null || muzzlePos == null) return;
        
        // Create new laser bullet
        GameObject bulletObj = Instantiate(bulletPrefab, muzzlePos.position, Quaternion.identity);
        currentLaserBullet = bulletObj.GetComponent<LaserBullet>();
        
        if (currentLaserBullet != null)
        {
            // Configure laser bullet properties
            currentLaserBullet.speed = laserBulletSpeed;
            currentLaserBullet.maxDistance = maxLaserDistance;
            currentLaserBullet.SetSpeed(direction);
        }
        
        // Only play shoot animation on initial fire
        if (animator != null && initialFire)
            animator.SetTrigger("Shoot");
            
        if (shootSound != null && initialFire)
            AudioSource.PlayClipAtPoint(shootSound, muzzlePos.position);
    }

    private void UpdateLaserBeam()
    {
        if (currentLaserBullet == null || currentLaserBullet.IsDestroyed())
        {
            // If bullet doesn't exist or was destroyed, create a new one
            Fire();
            return;
        }
        
        // Use bullet position as endpoint
        laserEndPoint = currentLaserBullet.transform.position;
        
        // Update beam positions
        laserBeam.SetPosition(0, muzzlePos.position);
        laserBeam.SetPosition(1, laserEndPoint);
        
        // Update beam particles if they exist
        if (beamParticles != null)
        {
            beamParticles.transform.position = laserEndPoint;
        }
        
        // Deal damage to enemies along the beam
        DealDamageAlongBeam();
    }

    private void DealDamageAlongBeam()
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(muzzlePos.position, direction, Vector2.Distance(muzzlePos.position, laserEndPoint));
        foreach (var hit in hits)
        {
            Character character = hit.collider.GetComponent<Character>();
            if (character != null)
            {
                character.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up the material we created
        if (laserBeam != null && laserBeam.material != null)
        {
            Destroy(laserBeam.material);
        }
    }
}