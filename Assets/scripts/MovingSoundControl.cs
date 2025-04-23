using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSoundControl : MonoBehaviour
{

	float dirX;
	[SerializeField]
	Rigidbody2D rb;
	AudioSource audioSrc;
	bool isMoving = false;
	float moveSpeed = 5f;

	// Use this for initialization
	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		audioSrc = GetComponent<AudioSource>();
	}

	// Update is called once per frame
	void Update()
	{
		dirX = Input.GetAxis("Horizontal") * moveSpeed;

		if (rb.linearVelocity.x != 0)
			isMoving = true;
		else
			isMoving = false;

		if (isMoving)
		{
			if (!audioSrc.isPlaying)
				audioSrc.Play();
		}
		else
			audioSrc.Stop();
	}

	void FixedUpdate()
	{
		rb.linearVelocity = new Vector2(dirX, rb.linearVelocity.y);
	}
}