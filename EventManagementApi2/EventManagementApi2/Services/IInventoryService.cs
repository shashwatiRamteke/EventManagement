namespace EventManagementApi2.Services;

/// <summary>
/// Result of an attempt to reserve tickets from the inventory.
/// </summary>
public enum ReservationStatus
{
    Reserved,
    InsufficientInventory,
    NotSeeded
}

/// <summary>
/// The outcome of a reservation attempt. When <see cref="Status"/> is
/// <see cref="ReservationStatus.Reserved"/>, <see cref="HoldId"/> is populated and
/// must later be confirmed or released.
/// </summary>
public sealed record ReservationResult(ReservationStatus Status, string? HoldId, int Remaining);

/// <summary>
/// In-memory inventory that prevents overselling by using atomic reserve/hold operations.
/// Reservations are held for a limited time and auto-released if not confirmed
/// (e.g. the buyer abandons checkout).
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Seeds the available inventory for an event/category if it has not been seeded yet.
    /// Idempotent: existing counts are never overwritten.
    /// </summary>
    Task EnsureSeededAsync(int eventId, int categoryId, int capacity);

    /// <summary>
    /// Atomically reserves <paramref name="quantity"/> tickets and creates a time-limited hold.
    /// </summary>
    Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity);

    /// <summary>
    /// Confirms a hold after the purchase is persisted. The reserved inventory stays consumed.
    /// </summary>
    Task ConfirmAsync(string holdId);

    /// <summary>
    /// Releases a hold, returning the reserved inventory to the available pool.
    /// </summary>
    Task ReleaseAsync(string holdId);

    /// <summary>
    /// Releases any holds whose expiry time has passed, returning their inventory. Returns the
    /// number of holds released.
    /// </summary>
    Task<int> ReleaseExpiredHoldsAsync();
}
