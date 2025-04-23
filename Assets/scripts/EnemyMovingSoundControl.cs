using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMovingSoundControl : MonoBehaviour
{
	[Header("音效配置")] // 新增音效Header
	[SerializeField] private AudioClip MovingSound;     
	[SerializeField] private AudioSource audioSource; 
	float dirX;
	[SerializeField]
	Rigidbody2D rb;
	bool isMoving = false;
	float moveSpeed = 2f;

	// Use this for initialization
	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		if (audioSource == null)
			audioSource = GetComponent<AudioSource>();


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
			if (!audioSource.isPlaying)
				audioSource.Play();
		}
		else
			audioSource.Stop();
	}

	void FixedUpdate()
	{
		rb.linearVelocity = new Vector2(dirX, rb.linearVelocity.y);
	}
}
