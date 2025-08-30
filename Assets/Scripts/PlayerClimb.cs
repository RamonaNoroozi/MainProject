using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerClimb : MonoBehaviour
{
    public float climbSpeed = 4f;
    private bool isOnLadder = false;
    private bool isClimbing = false;
    private Rigidbody2D rb;
    private float originalGravity;
    private float verticalInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
    }

    void Update()
    {
        if (isOnLadder)
        {
            verticalInput = Input.GetAxisRaw("Vertical");

            if (Mathf.Abs(verticalInput) > 0.1f)
                isClimbing = true;
            else
                isClimbing = false;
        }
    }

    void FixedUpdate()
    {
        if (isClimbing && isOnLadder)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, verticalInput * climbSpeed);
            rb.gravityScale = 0f;
        }
        else
        {
            if (!isOnLadder)
            {
                rb.gravityScale = originalGravity;
            }
        }
    }

    public void SetOnLadder(bool value)
    {
        isOnLadder = value;

        if (!value)
        {
            isClimbing = false;
            rb.gravityScale = originalGravity;
        }
    }
}