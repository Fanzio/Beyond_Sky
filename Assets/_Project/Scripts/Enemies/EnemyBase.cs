using UnityEngine;

// Common base for everything the spawner can send in.
//
// It exists so one spawner can hold one list instead of three, which is what lets a
// single timer enforce the on-screen limit. With separate spawners each would keep its
// own timer and nothing would stop them all firing at once.
//
// Only three things need to be shared: how to hand over the references, how to switch on,
// and how to tell whether the visit is over.
public abstract class EnemyBase : MonoBehaviour
{
    public abstract void Setup(Transform playerTarget, BulletPool pool);

    // Which pool an enemy draws from is decided by the spawner, so a roster entry can
    // point a new enemy at its own ammunition without any change here.
    public abstract void Activate(Vector3 startPosition);
    public abstract void TakeBulletHit();
}