using UnityEngine;

namespace DiveProtocol.Builds
{
    /// <summary>
    /// Opt-in marker for doors that may be bypassed by Red Marrow blood debt.
    /// </summary>
    public sealed class BloodDebtDoorBypass : MonoBehaviour
    {
        [SerializeField] private bool allowBloodBypass = true;
        [SerializeField, Min(0)] private int hpCost = 8;
        [SerializeField] private string bypassPrompt = "Spend blood to force the door open.";

        public bool AllowBloodBypass => allowBloodBypass;
        public int HpCost => hpCost;
        public string BypassPrompt => string.IsNullOrWhiteSpace(bypassPrompt)
            ? "Spend blood to force the door open."
            : bypassPrompt;

        public void SetAllowBloodBypass(bool allow)
        {
            allowBloodBypass = allow;
        }
    }
}
