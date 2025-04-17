using UnityEngine;

public class ChestInteraction : MonoBehaviour
{
    
    private Animator mAnimator;
    public Transform player; // Reference to the player
    public float interactionDistance = 3f; // Distance within which the player can interact


    void Start()
    {
        mAnimator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Vector3.Distance(player.position, transform.position) <= interactionDistance && Input.GetKeyDown(KeyCode.E))
        {
            mAnimator.SetTrigger("Open");
        }
    }

}   