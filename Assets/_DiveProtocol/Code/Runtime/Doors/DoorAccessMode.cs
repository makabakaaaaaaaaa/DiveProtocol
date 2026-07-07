namespace DiveProtocol.Doors
{
    /// <summary>
    /// Runtime access rule used by DoorInteractable before requesting door motion.
    /// </summary>
    public enum DoorAccessMode
    {
        Unlocked,
        RequiresItem,
        OneWayLatch
    }
}
