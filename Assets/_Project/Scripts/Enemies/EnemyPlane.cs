using UnityEngine;

public enum EnemyState
{
    Enter,
    Match,
    Telegraph,
    Fire,
    Leave
}

// An enemy flies in from ahead, tracks the player's altitude, locks on, fires and leaves.
// The whole visit lasts about four seconds.
//
// Fairness is the entire design here. During Telegraph the enemy STOPS tracking, and the
// bullet then travels horizontally along the enemy's own altitude. That means the plane
// itself is the warning: no aiming line is needed, and the player works out the rule
// after two encounters.
//
// Tracking speed is deliberately lower than the player's vertical speed, so a player who
// commits to moving can always break the lock.
public class EnemyPlane : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color telegraphColor = Color.red;
    [SerializeField] private float flashSpeed = 10f;

    [Header("Timing")]
    [SerializeField] private float matchDuration = 1.2f;
    [SerializeField] private float telegraphDuration = 0.9f;
    [SerializeField] private float leaveDuration = 1.4f;

    [Header("Movement")]
    [SerializeField] private float enterSpeed = 28f;
    [SerializeField] private float holdX = 11f;
    [SerializeField] private float trackSpeed = 7f;
    [SerializeField] private float leaveSpeed = 36f;

    [Header("Band limits")]
    [SerializeField] private float minY = 12f;
    [SerializeField] private float maxY = 28f;

    [Header("Firing")]
    [SerializeField] private float bulletSpeed = 22f;

    private Transform target;
    private BulletPool bulletPool;
    private EnemyState state;
    private float stateTimer;

    public void Setup(Transform playerTarget, BulletPool pool)
    {
        target = playerTarget;
        bulletPool = pool;
    }
    public void TakeBulletHit()
    {
        gameObject.SetActive(false);
    }
    public void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        state = EnemyState.Enter;
        stateTimer = 0f;
        bodyRenderer.color = normalColor;
        gameObject.SetActive(true);
    }
    private void Update()
    {
        float deltaTime = Time.deltaTime;
        stateTimer = stateTimer + deltaTime;

        if (state == EnemyState.Enter)
        {
            UpdateEnter(deltaTime);
            return;
        }
        if (state == EnemyState.Match)
        {
            UpdateMatch(deltaTime);
            return;
        }
        if (state == EnemyState.Telegraph)
        {
            UpdateTelegraph();
            return;
        }
        if (state == EnemyState.Fire)
        {
            UpdateFire();
            return;
        }
        UpdateLeave(deltaTime);
    }
    private void UpdateEnter(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }
        ChangeState(EnemyState.Match);
    }
    private void UpdateMatch(float deltaTime)
    {
        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        float newY = Mathf.MoveTowards(transform.position.y, targetY, trackSpeed * deltaTime);
        SetY(newY);

        if (stateTimer < matchDuration)
        {
            return;
        }
        ChangeState(EnemyState.Telegraph);
    }
    private void UpdateTelegraph()
    {
        float blend = Mathf.PingPong(Time.time * flashSpeed, 1f);
        bodyRenderer.color = Color.Lerp(normalColor, telegraphColor, blend);

        if (stateTimer < telegraphDuration)
        {
            return;
        }
        ChangeState(EnemyState.Fire);
    }
    private void UpdateFire()
    {
        bodyRenderer.color = normalColor;
        FireBullet();
        ChangeState(EnemyState.Leave);
    }
    private void UpdateLeave(float deltaTime)
    {
        MoveBy(leaveSpeed * deltaTime, leaveSpeed * 0.4f * deltaTime);

        if (stateTimer < leaveDuration)
        {
            return;
        }
        gameObject.SetActive(false);
    }
    private void FireBullet()
    {
        if (bulletPool == null)
        {
            return;
        }

        Bullet bullet = bulletPool.GetBullet();

        if (bullet == null)
        {
            return;
        }
        bullet.Launch(transform.position, Vector2.left, bulletSpeed);
    }
    private void ChangeState(EnemyState nextState)
    {
        state = nextState;
        stateTimer = 0f;
    }
    private void MoveBy(float deltaX, float deltaY)
    {
        Vector3 position = transform.position;
        position.x = position.x + deltaX;
        position.y = position.y + deltaY;
        transform.position = position;
    }
    private void SetY(float y)
    {
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;
    }
}