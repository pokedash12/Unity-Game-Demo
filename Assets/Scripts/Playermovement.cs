using UnityEngine;
using UnityEngine.InputSystem;

public class Playermovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    private Rigidbody2D rb;

    private Vector2 moveInput;
    private Animator animator;

    private bool playingFootsteps = false;

    public float footstepSpeed = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        if (ItemManager.Instance != null && ItemManager.Instance.hasStoredPosition)
        {
        // Snap back to where we were
            transform.position = ItemManager.Instance.lastOverworldPosition;
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (PauseController.IsGamePaused)
        {
            rb.linearVelocity = Vector2.zero;
            animator.SetBool("moving", false);
            StopFootsteps();
            return;
        }
        rb.linearVelocity = moveInput * moveSpeed;
        animator.SetBool("moving", rb.linearVelocity.magnitude > 0);

        if(rb.linearVelocity.magnitude > 0 && !playingFootsteps)
        {
            StartFootsteps();
        }
        else if(rb.linearVelocity.magnitude == 0)
        {
            StopFootsteps();
        }
    }

    public void TransitionToBattle()
    {
        // Save current position before the scene disappears
        ItemManager.Instance.lastOverworldPosition = transform.position;
        ItemManager.Instance.hasStoredPosition = true;
    }

    public void Move(InputAction.CallbackContext context)
{
    // READ the value first
    moveInput = context.ReadValue<Vector2>();

    // ONLY update animator parameters if the game is NOT paused
    if (!PauseController.IsGamePaused)
    {
        if (context.canceled)
        {
            animator.SetBool("moving", false);
            // We only update LastInput if we were actually moving before stopping
            // This prevents the 'snapping to front' issue
        }
        else
        {
            animator.SetBool("moving", true);
            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);

            // Update the "Last" values while moving so they are ready 
            // when the player stops or the game pauses
            animator.SetFloat("LastInputX", moveInput.x);
            animator.SetFloat("LastInputY", moveInput.y);
        }
    }
}

    public void StopFootsteps()
    {
        playingFootsteps = false;
        CancelInvoke(nameof(playFootstep));
    }

    public void StartFootsteps()
    {
        playingFootsteps = true;
        InvokeRepeating(nameof(playFootstep), 0f, footstepSpeed);
    }

    public void playFootstep()
    {
        SoundEffectManager.Play("Walking", true);
    }
}
