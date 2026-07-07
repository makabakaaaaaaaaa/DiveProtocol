namespace DiveProtocol
{
    /// <summary>
    /// Stores the single pending scene transition profile for the loading scene to consume.
    /// </summary>
    public static class SceneTransitionContext
    {
        public static SceneTransitionProfile PendingProfile { get; private set; }
        public static bool HasPendingTransition => PendingProfile != null;

        /// <summary>
        /// Replaces the pending transition with the provided profile.
        /// </summary>
        public static void SetPendingTransition(SceneTransitionProfile profile)
        {
            PendingProfile = profile;
        }

        /// <summary>
        /// Clears the pending transition request.
        /// </summary>
        public static void Clear()
        {
            PendingProfile = null;
        }
    }
}
