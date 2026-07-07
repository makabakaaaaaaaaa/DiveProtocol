using UnityEngine;

namespace DiveProtocol
{
    /// <summary>Marks the position and facing used when entering a playable level.</summary>
    public sealed class PlayerSpawnPoint : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 1.5f);
        }
    }
}
