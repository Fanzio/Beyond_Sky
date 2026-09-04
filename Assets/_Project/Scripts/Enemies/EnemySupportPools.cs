// Implemented only by enemies that need the special projectile pools already
// present in the scene. Ordinary enemies remain unchanged.
public interface IEnemySupportPoolReceiver
{
    void SetupSupportPools(
        ExplosiveMissilePool explosiveMissilePool,
        AerialMinePool aerialMinePool);
}
