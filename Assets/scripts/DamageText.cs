using UnityEngine;
using TMPro;

[RequireComponent(typeof(Animator))]
public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float floatHeight = 1f;
    [SerializeField] private float duration = 1f;

    private Vector3 startPosition;
    private float timer = 0f;

    public void Initialize(int damage, Vector3 position)
    {
        transform.position = position;
        damageText.text = damage.ToString();
        startPosition = position;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // ������λ�ã������ƶ���
        float progress = timer / duration;
        Vector3 newPosition = startPosition + new Vector3(0, floatHeight * progress, 0);
        transform.position = newPosition;

        // ������ʧЧ��
        Color color = damageText.color;
        color.a = 1 - progress;
        damageText.color = color;

        // ��������������
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}