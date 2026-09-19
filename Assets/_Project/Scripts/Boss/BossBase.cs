using UnityEngine;

public enum BossLifeState
{
    Idle,
    Enter,
    Fighting,
    Defeated
}
public abstract class BossBase : MonoBehaviour
{
    [Header("Warning visual")]
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Fight length")]
    [SerializeField] private float totalTime = 24f;
    [SerializeField] private float timeLostPerBullet = 3f;

    [Header("Entry")]
    [SerializeField] private float enterSpeed = 22f;
    [SerializeField] private float holdX = 12f;

    [Header("Defeat")]
    [SerializeField] private float defeatDuration = 1.2f;
    [SerializeField] private float defeatSpinSpeed = 420f;
    [SerializeField] private float defeatFallSpeed = 9f;

    private BossLifeState lifeState = BossLifeState.Idle;
    private Transform target;
    private BulletPool normalBulletPool;
    private float lifeStateTimer;
    private float timeRemaining;
    private bool isFinished;

    public bool IsFinished
    {
        get { return isFinished; }
    }

    public bool IsFighting
    {
        get { return lifeState == BossLifeState.Fighting; }
    }

    public bool IsDefeated
    {
        get { return lifeState == BossLifeState.Defeated; }
    }

    public float NormalizedTime
    {
        get
        {
            float safeTotal = Mathf.Max(0.0001f, totalTime);
            return Mathf.Clamp01(timeRemaining / safeTotal);
        }
    }

    protected Transform Target
    {
        get { return target; }
    }

    protected BulletPool NormalBulletPool
    {
        get { return normalBulletPool; }
    }

    protected Color NormalColor
    {
        get { return normalColor; }
    }

    public void Setup(Transform playerTarget, BulletPool bulletPool)
    {
        target = playerTarget;
        normalBulletPool = bulletPool;
        OnSetupComplete();
    }

    public void Activate(Vector3 startPosition)
    {
        transform.position = startPosition;
        transform.rotation = Quaternion.identity;

        timeRemaining = totalTime;
        isFinished = false;
        lifeStateTimer = 0f;
        lifeState = BossLifeState.Enter;

        OnBossActivated();
        ApplyBodyColor(normalColor);
        gameObject.SetActive(true);
    }

    public void TakeBulletHit()
    {
        if (IsFighting == false)
        {
            return;
        }

        timeRemaining = timeRemaining - timeLostPerBullet;
        AudioSignals.Play(GameSound.BossHit);

        if (timeRemaining > 0f)
        {
            return;
        }

        BeginDefeat();
    }

    protected virtual void Update()
    {
        float deltaTime = Time.deltaTime;
        lifeStateTimer = lifeStateTimer + deltaTime;

        if (lifeState == BossLifeState.Enter)
        {
            UpdateEntry(deltaTime);
            UpdateVisual();
            return;
        }

        if (lifeState == BossLifeState.Defeated)
        {
            UpdateDefeat(deltaTime);
            return;
        }

        if (lifeState != BossLifeState.Fighting)
        {
            return;
        }

        DrainTimer(deltaTime);

        if (lifeState != BossLifeState.Fighting)
        {
            return;
        }

        TickFight(deltaTime);
        UpdateVisual();
    }

    protected virtual void OnSetupComplete()
    {
    }

    protected virtual void OnBossActivated()
    {
    }

    protected virtual void OnFightStarted()
    {
    }

    protected virtual void OnDefeatStarted()
    {
    }

    protected abstract void TickFight(float deltaTime);

    protected virtual Color GetFightColor()
    {
        return normalColor;
    }

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

        float pointY = GetPointPosition(point).y;
        MoveReferenceYToTarget(pointY, speed, minY, maxY, deltaTime);
    }

    protected void TrackPointPairToTargetY(
        Transform firstPoint,
        Transform secondPoint,
        float speed,
        float minY,
        float maxY,
        float deltaTime)
    {
        if (target == null)
        {
            return;
        }

        float referenceY = transform.position.y;

        if (firstPoint != null && secondPoint != null)
        {
            referenceY = (firstPoint.position.y + secondPoint.position.y) * 0.5f;
        }
        else if (firstPoint != null)
        {
            referenceY = firstPoint.position.y;
        }
        else if (secondPoint != null)
        {
            referenceY = secondPoint.position.y;
        }

        MoveReferenceYToTarget(referenceY, speed, minY, maxY, deltaTime);
    }

    protected Vector3 GetPointPosition(Transform point)
    {
        if (point != null)
        {
            return point.position;
        }

        return transform.position;
    }

    protected Vector3 GetTargetPosition(
        Vector3 fallbackOrigin,
        float minY,
        float maxY)
    {
        if (target != null)
        {
            float targetY = Mathf.Clamp(target.position.y, minY, maxY);
            return new Vector3(target.position.x, targetY, 0f);
        }

        return fallbackOrigin + Vector3.left * 20f;
    }

    protected Color FlashBetween(Color from, Color to, float speed)
    {
        float blend = Mathf.PingPong(Time.time * speed, 1f);
        return Color.Lerp(from, to, blend);
    }

    private void MoveReferenceYToTarget(
        float referenceY,
        float speed,
        float minY,
        float maxY,
        float deltaTime)
    {
        float targetY = Mathf.Clamp(target.position.y, minY, maxY);
        float distanceToTarget = targetY - referenceY;
        float movement = Mathf.MoveTowards(
            0f,
            distanceToTarget,
            speed * deltaTime);
        MoveBy(0f, movement);
    }

    private void UpdateEntry(float deltaTime)
    {
        MoveBy(-enterSpeed * deltaTime, 0f);

        if (transform.position.x > holdX)
        {
            return;
        }

        Vector3 position = transform.position;
        position.x = holdX;
        transform.position = position;

        lifeState = BossLifeState.Fighting;
        lifeStateTimer = 0f;
        OnFightStarted();
    }

    private void DrainTimer(float deltaTime)
    {
        timeRemaining = timeRemaining - deltaTime;

        if (timeRemaining > 0f)
        {
            return;
        }

        BeginDefeat();
    }

    private void BeginDefeat()
    {
        if (lifeState == BossLifeState.Defeated)
        {
            return;
        }

        timeRemaining = 0f;
        lifeState = BossLifeState.Defeated;
        AudioSignals.Play(GameSound.BossDefeated);
        lifeStateTimer = 0f;
        ApplyBodyColor(normalColor);
        OnDefeatStarted();
    }

    private void UpdateDefeat(float deltaTime)
    {
        transform.Rotate(0f, 0f, defeatSpinSpeed * deltaTime);
        MoveBy(-defeatFallSpeed * 0.4f * deltaTime, -defeatFallSpeed * deltaTime);

        if (lifeStateTimer < defeatDuration)
        {
            return;
        }

        isFinished = true;
        gameObject.SetActive(false);
    }

    private void UpdateVisual()
    {
        ApplyBodyColor(GetFightColor());
    }

    private void ApplyBodyColor(Color color)
    {
        if (bodyRenderer == null)
        {
            return;
        }

        bodyRenderer.color = color;
    }
}
