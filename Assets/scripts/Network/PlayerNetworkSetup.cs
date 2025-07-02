using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSetup : MonoBehaviourPun
{
    // 提前獲取武器的引用，以提高效率
    private gun currentGun;

    void Awake()
    {
        currentGun = GetComponentInChildren<gun>();
    }

    /// <summary>
    /// RPC: 處理單發子彈武器的開火事件。
    /// </summary>
    [PunRPC]
    public void RPC_FireSingle(Vector3 position, Quaternion rotation, float damage)
    {
        if (currentGun == null || currentGun.bulletPrefab == null) return;

        // 所有客戶端都在本地生成一顆視覺子彈
        GameObject bulletObj = Instantiate(currentGun.bulletPrefab, position, rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // 初始化子彈，告訴它傷害值和誰是它的主人
            bullet.Initialize(damage, this.photonView);
        }
    }

    /// <summary>
    /// RPC: 處理霰彈槍的開火事件。
    /// </summary>
    [PunRPC]
    public void RPC_FireShotgun(Vector3 position, Quaternion baseRotation, float damagePerPellet, int pelletCount, float spreadAngle)
    {
        if (currentGun == null || currentGun.bulletPrefab == null) return;

        // 所有客戶端都在本地模擬完全一樣的擴散模式
        for (int i = 0; i < pelletCount; i++)
        {
            float angleOffset = (pelletCount > 1) ? (-spreadAngle / 2) + (spreadAngle / (pelletCount - 1)) * i : 0f;
            Quaternion pelletRotation = baseRotation * Quaternion.Euler(0, 0, angleOffset);

            GameObject bulletObj = Instantiate(currentGun.bulletPrefab, position, pelletRotation);
            Bullet bullet = bulletObj.GetComponent<Bullet>();
            if (bullet != null)
            {
                bullet.Initialize(damagePerPellet, this.photonView);
            }
        }
    }

    /// <summary>
    /// RPC: 處理所有武器通用的視覺和聽覺效果。
    /// </summary>
    [PunRPC]
    public void PlayFireEffects(Vector3 position)
    {
        if (currentGun == null) return;

        // 觸發動畫
        if (currentGun.GetComponent<Animator>() != null)
            currentGun.GetComponent<Animator>().SetTrigger("Shoot");

        // 播放音效
        if (currentGun.shootSound != null)
            AudioSource.PlayClipAtPoint(currentGun.shootSound, position);

        // 生成彈殼
        if (currentGun.shellPrefab != null && currentGun.ShellPosition != null)
            Instantiate(currentGun.shellPrefab, currentGun.ShellPosition.position, currentGun.ShellPosition.rotation);
    }

    /// <summary>
    /// RPC: 同步雷射槍的開火狀態 (開始/停止)。
    /// </summary>
    [PunRPC]
    public void SetFiringState(bool firing)
    {
        LaserGun laser = currentGun as LaserGun;
        if (laser == null) return;

        laser.isFiring = firing;
        if (firing)
        {
            if (laser.muzzleFlash != null) laser.muzzleFlash.Play();
            if (laser.beamImpactParticles != null) laser.beamImpactParticles.Play();
            laser.laserBeam.enabled = true;
        }
        else
        {
            if (laser.muzzleFlash != null) laser.muzzleFlash.Stop();
            if (laser.beamImpactParticles != null) laser.beamImpactParticles.Stop();
            laser.laserBeam.enabled = false;
        }
    }

    [PunRPC]
    public void UpdateLaserVisuals(Vector3 startPoint, Vector3 endPoint)
    {
        LaserGun laser = currentGun as LaserGun;
        if (laser == null || !laser.isFiring) return;

        laser.laserBeam.SetPosition(0, startPoint);
        laser.laserBeam.SetPosition(1, endPoint);

        if (laser.beamImpactParticles != null)
        {
            laser.beamImpactParticles.transform.position = endPoint;
        }
    }
}
