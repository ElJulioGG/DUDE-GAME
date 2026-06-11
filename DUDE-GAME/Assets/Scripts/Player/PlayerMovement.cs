using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private PlayerStats playerStats;

    // How long the movement correction is suspended after a knockback so the impulse
    // can carry. 0 = original game feel: the correction fights knockback immediately,
    // keeping distances short. Raising this makes hits launch players further.
    [SerializeField] private float knockbackFreeFlightTime = 0f;

    public Vector2 moveInput;
    private Rigidbody2D rb;
    private int playerIndex;
    private NetworkObject _netObj;
    private float _diagLogTimer;

    private float _speedMultiplier = 1f;
    private float _boostEndTime;
    private float _knockbackEndTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (playerStats == null) playerStats = GetComponent<PlayerStats>();
        if (playerStats != null) playerIndex = playerStats.GetPlayerIndex();
        _netObj = GetComponent<NetworkObject>();
    }

    public void SetPlayerIndex(int index)
    {
        playerIndex = index;
    }

    public int GetPlayerIndex()
    {
        return playerIndex;
    }

    // Pass a duration to override; otherwise the serialized knockbackFreeFlightTime
    // applies (default 0 = original behavior, no free flight).
    public void SetKnockbackWindow(float duration = -1f)
    {
        _knockbackEndTime = Time.time + (duration >= 0f ? duration : knockbackFreeFlightTime);
    }

    void FixedUpdate()
    {
        if (!GameManager.instance.playersCanMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Log once every 2 seconds so we can see what state each machine thinks each player is in.
        _diagLogTimer -= Time.fixedDeltaTime;
        if (_diagLogTimer <= 0f)
        {
            _diagLogTimer = 2f;
            bool isOnline = GameSession.IsOnline;
            bool isOwner = _netObj != null && _netObj.IsOwner;
            Debug.Log($"[MOV-DIAG] {name} idx={playerIndex} online={isOnline} netObj={(_netObj != null ? "OK" : "NULL")} IsOwner={isOwner} moveInput={moveInput} vel={rb.linearVelocity}");
        }

        // In online play, non-owner machines let NetworkTransform drive the position.
        // Applying physics here would fight the incoming sync and cause jitter.
        if (GameSession.IsOnline && _netObj != null && !_netObj.IsOwner)
            return;

        if (_speedMultiplier > 1f && Time.time >= _boostEndTime)
            _speedMultiplier = 1f;

        // Let knockback impulses play out before correcting back to input velocity.
        if (Time.time < _knockbackEndTime)
            return;

        Vector2 desiredVelocity = moveInput * maxSpeed * _speedMultiplier;
        Vector2 velocityDiff = desiredVelocity - rb.linearVelocity;

        rb.AddForce(velocityDiff * 10f, ForceMode2D.Force);
    }

    public void SetInputVector(Vector2 direction)
    {
        moveInput = direction;
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        _speedMultiplier = multiplier;
        _boostEndTime = Time.time + duration;
    }
}
