using UnityEngine;

public class ParticleEffect : MonoBehaviour
{
    private ParticleSystem ps;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        ps.Play();
        Destroy(gameObject, ps.main.duration);
    }
}


