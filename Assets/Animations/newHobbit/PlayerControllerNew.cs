using System;
using System.Collections;
using Unity.Jobs;
using Unity.VisualScripting;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerControllerNew : MonoBehaviour, IPlayerInputBlocker, IPlayerController
{
    public bool isInputBlocked { get; set; } = false;
    [SerializeField] private ScriptableStats _stats;
    private Rigidbody2D _rb;
    private CapsuleCollider2D _col;
    private FrameInput _frameInput;
    private Vector2 _frameVelocity;
    private bool _cachedQueryStartInColliders;
    [HideInInspector] public bool IsClimbing = false;

    #region Interface  

    public Vector2 FrameInput => _frameInput.Move;
    public event Action<bool, float> GroundedChanged;
    public event Action Jumped;
    public event Action Attacked;

    #endregion

    private float _time;
    private Vector2 _velocityBoost = Vector2.zero;



    [Header("Shooting")]
    public GameObject bulletPrefab;
    private GameObject activeBullet = null;
    public Transform firePoint;
    public float bulletSpeed = 15f;
    public float bulletSpawnDelay = 0.2f;

    public Camera hobbitCamera;
    public float shakeDuration;
    public float shakeMagnitude;

    [Header("Attack")]
    public int attackDamage = 1;

    public WeaponUIIndicator weaponUIIndicator;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<CapsuleCollider2D>();

        _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
    }

    void Update()
    {
        _time += Time.deltaTime;
        if (isInputBlocked)
        {
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            //animator.SetFloat("Yvelocity", rb.linearVelocity.y);
            //animator.SetFloat("magnitude", 0);
            return;
        }
        // Check for climb input while on ladder
        if (_isOnLadder)
        {
            float climbInput = 0f;

            // W key / Jump key climbs up
            if (_frameInput.JumpDown)
                climbInput = 1f;

            // S key / Down arrow key climbs down
            if (_frameInput.Move.y < 0f)
                climbInput = _frameInput.Move.y;

            // If any vertical input
            if (Mathf.Abs(climbInput) > 0.1f)
            {
                if (!_isClimbing)
                    StartClimbing();

                _rb.gravityScale = 0f;
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, climbInput * climbSpeed);
            }
            else if (_isClimbing)
            {
                // Stop moving on ladder when no input
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
            }
        }
        else if (_isClimbing)
        {
            StopClimbing();
        }

    }
    private void FixedUpdate()
    {
        CheckCollisions();

        HandleJump();
        HandleDirection();
        HandleGravity();
        ApplyMovement();

    }
    public void Move(InputAction.CallbackContext context)
    {
        if (isInputBlocked) return;
        _frameInput.Move = context.ReadValue<Vector2>();
    }
public void Jump(InputAction.CallbackContext context)
    {
        Debug.Log("jump was pressed!");
        if (isInputBlocked) return;
        if (context.started)
        {
            _frameInput.JumpDown = true;
            _jumpToConsume = true;
            _timeJumpWasPressed = _time;

        }
        if (context.performed)
        {
            _frameInput.JumpHeld = true;
        }
        if (context.canceled)
        {
            _frameInput.JumpHeld = false;
        }
    }
    public void Shoot(InputAction.CallbackContext context)
    {
        if (isInputBlocked || !context.performed) return;
        Attacked?.Invoke();
        StartCoroutine(DelayedBulletSpawn());

    }

    private IEnumerator DelayedBulletSpawn()
    {
        yield return new WaitForSeconds(bulletSpawnDelay);

        if (activeBullet != null) yield break;

        Vector3 mousePosition = hobbitCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 shootDirection = (mousePosition - firePoint.position).normalized;

        activeBullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D rb = activeBullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = shootDirection * bulletSpeed;
        }

        Bullet bulletScript = activeBullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.damage = attackDamage;
            bulletScript.OnDestroyed += HandleBulletDestroyed;
        }


    }

    private void HandleBulletDestroyed()
    {
        activeBullet = null;
    }
    #region Collisions  

    private float _frameLeftGrounded = float.MinValue;
    private bool _grounded;

    private void CheckCollisions()
    {
        Physics2D.queriesStartInColliders = false;

        // Ground and Ceiling  
        bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer);
        bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0, Vector2.up, _stats.GrounderDistance, ~_stats.PlayerLayer);

        // Hit a Ceiling  
        if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

        // Landed on the Ground  
        if (!_grounded && groundHit)
        {
            //Debug.Log("Grounded");
            _grounded = true;
            _coyoteUsable = true;
            _bufferedJumpUsable = true;
            _endedJumpEarly = false;
            jumpsRemaining = maxJumps;
            GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
        }
        // Left the Ground  
        else if (_grounded && !groundHit)
        {
            _grounded = false;
            _frameLeftGrounded = _time;
            GroundedChanged?.Invoke(false, 0);
        }

        Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
    }

    #endregion
     #region Jumping  

    private bool _jumpToConsume;
    private bool _bufferedJumpUsable;
    private bool _endedJumpEarly;
    private bool _coyoteUsable;
    private float _timeJumpWasPressed;
    [SerializeField] private int maxJumps = 2;
    private int jumpsRemaining;


    private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
    private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

    private void HandleJump()
    {
        if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0)
            _endedJumpEarly = true;

        if (!_jumpToConsume && !HasBufferedJump) return;

        // First jump: Ground or Coyote
        if ((_grounded || CanUseCoyote) && jumpsRemaining > 0)
        {
            ExecuteJump();
        }
        // Extra jumps: allow in-air if we still have jumps left
        else if (!_grounded && jumpsRemaining > 0)
        {
            ExecuteJump();
        }

        _jumpToConsume = false;
}


    private void ExecuteJump()
    {
        //Debug.Log("Execute called!");
        _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _coyoteUsable = false;
        jumpsRemaining--;
        _frameVelocity.y = _stats.JumpPower;
        Jumped?.Invoke();
    }

    #endregion
    #region Climb

    [SerializeField] private float climbSpeed = 4f;
    private bool _isOnLadder = false;
    private bool _isClimbing = false;
    private float originalGravity;
    private void StartClimbing()
    {
        _isClimbing = true;
        IsClimbing = true; // This was your public field
        originalGravity = _rb.gravityScale; // Store original gravity scale
        _rb.gravityScale = 0f;

    }

    private void StopClimbing()
    {
        _isClimbing = false;
        IsClimbing = false;
        _rb.gravityScale = originalGravity; // or original gravity
    }
    public void SetOnLadder(bool value)
    {
        _isOnLadder = value;

        if (!value && _isClimbing)
        {
            StopClimbing();
        }
    }



    #endregion  
    #region Horizontal  

    private void HandleDirection()
    {
        if (_frameInput.Move.x == 0)
        {
            var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
        }
        else
        {
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region Gravity  

    private void HandleGravity()
    {
        if (_grounded && _frameVelocity.y <= 0f)
        {
            _frameVelocity.y = _stats.GroundingForce;
        }
        else
        {
            var inAirGravity = _stats.FallAcceleration;
            if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
            _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
        }
    }

    #endregion

    #region VelocityBoost 

    public void ExecuteBounce(float bouncePower){
            _endedJumpEarly = false;
        _timeJumpWasPressed = 0;
        _bufferedJumpUsable = false;
        _coyoteUsable = false;
        _frameVelocity.y = bouncePower;
        Jumped?.Invoke();
    }
    #endregion


    private void ApplyMovement()
{
    if (!IsClimbing)
    {
        _rb.linearVelocity = _frameVelocity;
    }
}

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
    }
#endif

}
public interface IPlayerController
{
    public event Action<bool, float> GroundedChanged;

    public event Action Jumped;
    public Vector2 FrameInput { get; }  
    public event Action Attacked;
    } 
  
    public struct FrameInput  
    {  
        public bool JumpDown;  
        public bool JumpHeld;  
        public Vector2 Move;  
    }  
  
    

