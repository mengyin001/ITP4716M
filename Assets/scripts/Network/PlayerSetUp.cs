using UnityEngine;

public class PlayerSetUp : MonoBehaviour
{
    public PlayerMovement movement;
    public GameObject camera;

    public void IsLocalPlayer()
    {
        movement.enabled = true;
        camera.SetActive(true);
    }
}
