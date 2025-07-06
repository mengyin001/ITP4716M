// Add new DamagePopup.cs file
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public TextMeshPro textMesh;
    public float lifetime = 0.5f;
    public float floatSpeed = 1f;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        // Float upward
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade out over time
        float alpha = Mathf.Clamp01(lifetime - Time.deltaTime) / lifetime;
        textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
    }

    public void SetDamage(float damage)
    {
        textMesh.text = damage.ToString("F0");

        // Visual customization based on damage
        textMesh.fontSize = Mathf.Clamp(4 + damage * 0.5f, 6, 12);
        textMesh.color = damage > 15 ? Color.red :
                        damage > 10 ? new Color(1, 0.5f, 0) : // Orange
                        Color.yellow;
    }
}