using System.Collections.Concurrent;

namespace EventManagementApi2.Services;

/// <summary>
/// Thread-safe, in-process implementation of <see cref="IInventoryService"/>. It mirrors the
/// semantics of a Redis reservation/hold pattern but keeps all state in memory, so no external
/// server is required.
///
/// Overselling is prevented by guarding each inventory slot with its own lock: the check
/// (is there enough?) and the decrement happen as a single atomic operation, so two concurrent
/// purchases can never both take the last tickets.
///
/// Registered as a singleton so the counters are shared across all requests.
/// </summary>
public sealed class InMemoryInventoryService : IInventoryService
{
    private sealed class Slot
    {
        public int Available;
        public readonly object Gate = new();
    }

    private sealed record Hold(string SlotKey, int Quantity, DateTimeOffset ExpiresAt);

    private readonly ConcurrentDictionary<string, Slot> _slots = new();
    private readonly ConcurrentDictionary<string, Hold> _holds = new();
    private readonly ILogger<InMemoryInventoryService> _logger;
    private readonly TimeSpan _holdDuration;

    public InMemoryInventoryService(IConfiguration configuration, ILogger<InMemoryInventoryService> logger)
    {
        _logger = logger;
        var seconds = configuration.GetValue<int?>("Inventory:HoldDurationSeconds") ?? 600;
        _holdDuration = TimeSpan.FromSeconds(seconds);
    }

    private static string SlotKey(int eventId, int categoryId) => $"{eventId}:{categoryId}";

    public Task EnsureSeededAsync(int eventId, int categoryId, int capacity)
    {
        // Only seeds when the slot is absent, so it is safe to call repeatedly.
        _slots.GetOrAdd(SlotKey(eventId, categoryId), _ => new Slot { Available = Math.Max(0, capacity) });
        return Task.CompletedTask;
    }

    public Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity)
    {
        var key = SlotKey(eventId, categoryId);

        if (!_slots.TryGetValue(key, out var slot))
            return Task.FromResult(new ReservationResult(ReservationStatus.NotSeeded, null, 0));

        int remaining;
        lock (slot.Gate)
        {
            if (slot.Available < quantity)
                return Task.FromResult(new ReservationResult(ReservationStatus.InsufficientInventory, null, slot.Available));

            slot.Available -= quantity;
            remaining = slot.Available;
        }

        var holdId = Guid.NewGuid().ToString("N");
        _holds[holdId] = new Hold(key, quantity, DateTimeOffset.UtcNow.Add(_holdDuration));

        return Task.FromResult(new ReservationResult(ReservationStatus.Reserved, holdId, remaining));
    }

    public Task ConfirmAsync(string holdId)
    {
        // Inventory was already consumed on reserve; just drop the hold.
        _holds.TryRemove(holdId, out _);
        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string holdId)
    {
        if (_holds.TryRemove(holdId, out var hold) && _slots.TryGetValue(hold.SlotKey, out var slot))
        {
            lock (slot.Gate)
            {
                slot.Available += hold.Quantity;
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> ReleaseExpiredHoldsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredIds = _holds
            .Where(kv => kv.Value.ExpiresAt <= now)
            .Select(kv => kv.Key)
            .ToList();

        var released = 0;
        foreach (var holdId in expiredIds)
        {
            if (_holds.TryRemove(holdId, out var hold) && _slots.TryGetValue(hold.SlotKey, out var slot))
            {
                lock (slot.Gate)
                {
                    slot.Available += hold.Quantity;
                }

                released++;
                _logger.LogInformation("Released expired hold {HoldId}", holdId);
            }
        }

        return Task.FromResult(released);
    }
}
