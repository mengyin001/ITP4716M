using UnityEngine;

public class Explosion : MonoBehaviour
{
    private Animator animator;
    private AnimatorStateInfo info;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        animator=GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.normalizedTime>=1){
            Destroy(gameObject);
        }
    }
}
