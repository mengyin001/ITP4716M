using Photon.Pun;
using UnityEngine;

public class PlayerNetworkSetup : MonoBehaviourPun
{
    // ��ǰ�@ȡ���������ã������Ч��
    private gun currentGun;

    void Awake()
    {
        currentGun = GetComponentInChildren<gun>();
    }

    /// <summary>
    /// RPC: ̎��ΰl�ӏ��������_���¼���
    /// </summary>
    [PunRPC]
    public void RPC_FireSingle(Vector3 position, Quaternion rotation, float damage)
    {
        if (currentGun == null || currentGun.bulletPrefab == null) return;

        // ���п͑��˶��ڱ�������һ�wҕ�X�ӏ�
        GameObject bulletObj = Instantiate(currentGun.bulletPrefab, position, rotation);
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // ��ʼ���ӏ������V������ֵ���l����������
            bullet.Initialize(damage, this.photonView);
        }
    }

    /// <summary>
    /// RPC: ̎�����������_���¼���
    /// </summary>
    [PunRPC]
    public void RPC_FireShotgun(Vector3 position, Quaternion baseRotation, float damagePerPellet, int pelletCount, float spreadAngle)
    {
        if (currentGun == null || currentGun.bulletPrefab == null) return;

        // ���п͑��˶��ڱ���ģ�M��ȫһ�ӵĔUɢģʽ
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
    /// RPC: ̎����������ͨ�õ�ҕ�X�� �XЧ����
    /// </summary>
    [PunRPC]
    public void PlayFireEffects(Vector3 position)
    {
        if (currentGun == null) return;

        // �|�l�Ӯ�
        if (currentGun.GetComponent<Animator>() != null)
            currentGun.GetComponent<Animator>().SetTrigger("Shoot");

        // ������Ч
        if (currentGun.shootSound != null)
            AudioSource.PlayClipAtPoint(currentGun.shootSound, position);

        // ���ɏ���
        if (currentGun.shellPrefab != null && currentGun.ShellPosition != null)
            Instantiate(currentGun.shellPrefab, currentGun.ShellPosition.position, currentGun.ShellPosition.rotation);
    }

    /// <summary>
    /// RPC: ͬ�����䘌���_���B (�_ʼ/ֹͣ)��
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
