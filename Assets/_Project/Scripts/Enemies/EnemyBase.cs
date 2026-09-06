using UnityEngine;

// Keeps shared references and movement helpers in one place for every pooled enemy.
public abstract class EnemyBase : MonoBehaviour
{
    private Transform target;
    private BulletPool normalBulletPool;
    private ExplosiveMissilePool missilePool;
    private AerialMinePool minePool;

    protected Transform Target
    {
        get { return target; }
    }

    protected BulletPool NormalBulletPool
    {
        get { return normalBulletPool; }
    }

    protected ExplosiveMissilePool MissilePool
    {
        get { return missilePool; }
    }

    protected AerialMinePool MinePool
    {
        get { return minePool; }
    }

    public virtual void Setup(Transform playerTarget, BulletPool pool)
    {
        target = playerTarget;
        normalBulletPool = pool;
    }

    public void SetupExplosiveMissilePool(ExplosiveMissilePool pool)
    {
        missilePool = pool;
    }

    public void SetupAerialMinePool(AerialMinePool pool)
    {
        minePool = pool;
    }

    public abstract void Activate(Vector3 startPosition);
    public abstract void TakeBulletHit();

    protected void MoveBy(float deltaX, float deltaY)
    {
        Vector3 position = transform.position;
        position.x = position.x + deltaX;
        position.y = position.y + deltaY;
        transform.position = position;
    }

    protected void SetY(float y)
    {
        Vector3 position = transform.position;
        position.y = y;
        transform.position = position;
    }

    protected void TrackTargetY(float speed, float minY, float maxY, float deltaTime)
    {
        TrackPointToTargetY(null, speed, minY, maxY, deltaTime);
    }

    protected void TrackPointToTargetY(
        Transform point,
        float speed,
        float minY,
        float maxY,
        float deltaTime)
    {
        if (target == null)
        {
            return;
        }

        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        float pointY = GetPointPosition(point).y;
        float distanceToTarget = targetY - pointY;
        float movement = Mathf.MoveTowards(
            0f,
            distanceToTarget,
            speed * deltaTime);
        MoveBy(0f, movement);
    }

    protected Vector3 GetPointPosition(Transform point)
    {
        if (point != null)
        {
            return point.position;
        }

        return transform.position;
    }

    protected Vector3 GetTargetPosition(Vector3 fallbackOrigin, float fallbackDistance)
    {
        if (target != null)
        {
            return target.position;
        }

        return fallbackOrigin + Vector3.left * fallbackDistance;
    }

    protected float PickRandomYAwayFromTarget(
        float firstLimit,
        float secondLimit,
        float minimumDistance)
    {
        float minimum = Mathf.Min(firstLimit, secondLimit);
        float maximum = Mathf.Max(firstLimit, secondLimit);

        if (target == null)
        {
            return Random.Range(minimum, maximum);
        }

        float safeDistance = Mathf.Max(0f, minimumDistance);
        float belowMaximum = Mathf.Min(maximum, target.position.y - safeDistance);
        float aboveMinimum = Mathf.Max(minimum, target.position.y + safeDistance);
        bool canSpawnBelow = belowMaximum > minimum;
        bool canSpawnAbove = aboveMinimum < maximum;

        if (canSpawnBelow && canSpawnAbove)
        {
            if (Random.value < 0.5f)
            {
                return Random.Range(minimum, belowMaximum);
            }

            return Random.Range(aboveMinimum, maximum);
        }

        if (canSpawnBelow)
        {
            return Random.Range(minimum, belowMaximum);
        }

        if (canSpawnAbove)
        {
            return Random.Range(aboveMinimum, maximum);
        }

        return Random.Range(minimum, maximum);
    }

    protected Color FlashBetween(Color from, Color to, float speed)
    {
        float blend = Mathf.PingPong(Time.time * speed, 1f);
        return Color.Lerp(from, to, blend);
    }

    protected void DeactivateEnemy()
    {
        gameObject.SetActive(false);
    }
}
