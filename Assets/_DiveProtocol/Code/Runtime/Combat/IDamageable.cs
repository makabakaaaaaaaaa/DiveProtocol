namespace DiveProtocol
{
    /// <summary>
    /// Runtime contract for objects that can receive damage.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(DamageInfo damageInfo);
    }
}
