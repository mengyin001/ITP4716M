using UnityEngine;
using Photon.Pun;

public class LaserGun : gun
{
    [Header("Laser Settings")]
    public LineRenderer laserBeam;
    public float laserWidth = 0.1f;
    public Color laserColor = Color.green;
    public float maxLaserDistance = 100f;
    public float damagePerSecond = 10f;
    public LayerMask hitLayer;

    [Header("Laser Visual Effects")]
    public ParticleSystem muzzleFlash;
    public ParticleSystem beamImpactParticles;

    [HideInInspector] public bool isFiring = false;

    protected override void Awake()
    {
        base.Awake();
        InitializeLaserBeam();
        if (muzzleFlash != null) muzzleFlash.Stop();
        if (beamImpactParticles != null) beamImpactParticles.Stop();
    }

    private void InitializeLaserBeam()
    {
        if (laserBeam == null) laserBeam = gameObject.AddComponent<LineRenderer>();
        laserBeam.startWidth = laserWidth;
        laserBeam.endWidth = laserWidth;
        laserBeam.material = new Material(Shader.Find("Sprites/Default"));
        laserBeam.startColor = laserColor;
        laserBeam.endColor = laserColor;
        laserBeam.positionCount = 2;
        laserBeam.enabled = false;
    }

    protected override void HandleShootingInput()
    {
        bool canFire = (healthSystem == null || healthSystem.HasEnoughEnergy(energyCostPerShot * Time.deltaTime)) &&
                       (DialogueSystem.Instance == null || !DialogueSystem.Instance.isDialogueActive);

        if (Input.GetButton("Fire1") && canFire)
        {
            if (!isFiring)
            {
                isFiring = true;
                parentPhotonView.RPC("SetFiringState", RpcTarget.AllBuffered, true);
            }
            if (healthSystem != null)
            {
                healthSystem.RPC_ConsumeEnergy(energyCostPerShot * Time.deltaTime);
            }
        }
        else if (isFiring)
        {
            isFiring = false;
            parentPhotonView.RPC("SetFiringState", RpcTarget.AllBuffered, false);
        }

        if (isFiring)
        {
            UpdateLaser();
        }
    }

    private void UpdateLaser()
    {
        RaycastHit2D hit = Physics2D.Raycast(muzzlePos.position, direction, maxLaserDistance, hitLayer);
        Vector2 hitPoint = hit.collider ? hit.point : (Vector2)muzzlePos.position + direction * maxLaserDistance;

        if (hit.collider && PhotonNetwork.IsMasterClient)
        {
            Character character = hit.collider.GetComponent<Character>();
            if (character != null)
            {
                character.photonView.RPC("TakeDamage", RpcTarget.All, damagePerSecond * Time.deltaTime);
            }
        }
        parentPhotonView.RPC("UpdateLaserVisuals", RpcTarget.All, muzzlePos.position, hitPoint);
    }

    // 雷射槍不使用傳統的 Fire 方法
    protected override void Fire() { }

    private void OnDestroy()
    {
        if (laserBeam != null && laserBeam.material != null)
        {
            Destroy(laserBeam.material);
        }
    }
}
