# InMemoryInventoryService: Seat Hold Logic Implementation

## Overview

The `InMemoryInventoryService` implements a **reservation/hold pattern** to prevent ticket overselling in concurrent scenarios. It mimics Redis-based reservation systems but operates entirely in-memory, making it perfect for development and testing without external dependencies.

## Key Problem Solved

### The Overselling Problem

In a typical ticket booking system without proper concurrency control:

```
Request A: Check available = 1  ✓
Request B: Check available = 1  ✓
Request A: Buy 1 ticket        ✓ (Available becomes 0)
Request B: Buy 1 ticket        ✓ (OVERSOLD! Should be 0 but sold 2)
```

The `InMemoryInventoryService` solves this by making the check-and-decrement **atomic** (indivisible).

---

## Architecture Components

### 1. **IInventoryService Interface**
Defines the contract for inventory management with five key operations:

```csharp
public interface IInventoryService
{
	Task EnsureSeededAsync(int eventId, int categoryId, int capacity);
	Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity);
	Task ConfirmAsync(string holdId);
	Task ReleaseAsync(string holdId);
	Task<int> ReleaseExpiredHoldsAsync();
}
```

### 2. **InMemoryInventoryService Implementation**
Thread-safe, singleton service that maintains in-memory state.

### 3. **HoldExpiryService**
Background service that automatically releases expired holds every 30 seconds.

---

## Core Data Structures

### Slot (Internal Class)
Represents available inventory for a specific event-category combination:

```csharp
private sealed class Slot
{
	public int Available;           // Current available tickets
	public readonly object Gate;    // Lock for thread-safe operations
}
```

**Key:** `"eventId:categoryId"` (e.g., `"1:3"` = Event 1, Category 3)

### Hold (Internal Record)
Represents a temporary reservation:

```csharp
private sealed record Hold(
	string SlotKey,         // Which slot the hold is for
	int Quantity,           // How many tickets are held
	DateTimeOffset ExpiresAt // When the hold expires
);
```

**Key:** Unique GUID (e.g., `"a3f4b8c2d1e9..."`)

### Storage Collections

```csharp
// Stores available inventory per event-category
private readonly ConcurrentDictionary<string, Slot> _slots = new();

// Stores active holds (reservations)
private readonly ConcurrentDictionary<string, Hold> _holds = new();
```

Both use `ConcurrentDictionary` for thread-safe access.

---

## How Seat Hold Logic Works

### Phase 1: Initialization - `EnsureSeededAsync`

**When:** Event is created or application starts

```csharp
await _inventory.EnsureSeededAsync(eventId: 1, categoryId: 3, capacity: 100);
```

**What happens:**
1. Creates a unique key: `"1:3"`
2. Initializes a `Slot` with `Available = 100`
3. Idempotent: Won't overwrite if already exists

```
_slots["1:3"] = Slot { Available: 100, Gate: <lock> }
```

---

### Phase 2: Reservation - `ReserveAsync` ⭐ (Core Logic)

**When:** User attempts to purchase tickets

```csharp
var result = await _inventory.ReserveAsync(eventId: 1, categoryId: 3, quantity: 2);
```

**Step-by-step execution:**

#### Step 1: Retrieve the Slot
```csharp
var key = SlotKey(1, 3); // "1:3"
if (!_slots.TryGetValue(key, out var slot))
	return NotSeeded;
```

#### Step 2: Atomic Check-and-Decrement (Thread-Safe!)
```csharp
int remaining;
lock (slot.Gate)  // 🔒 CRITICAL SECTION - Only one thread at a time
{
	// Check if enough tickets available
	if (slot.Available < quantity)
		return InsufficientInventory;

	// Decrement immediately
	slot.Available -= quantity;
	remaining = slot.Available;
}
```

**Why lock?** Ensures no other thread can check/modify `Available` simultaneously.

#### Step 3: Create a Hold
```csharp
var holdId = Guid.NewGuid().ToString("N");
var expiresAt = DateTimeOffset.UtcNow.Add(_holdDuration); // Default: 10 minutes

_holds[holdId] = new Hold(key, quantity, expiresAt);

return new ReservationResult(Reserved, holdId, remaining);
```

**Result:**
```
_slots["1:3"] = Slot { Available: 98 }  // Reduced immediately
_holds["abc123..."] = Hold { SlotKey: "1:3", Quantity: 2, ExpiresAt: <10 min from now> }
```

---

### Phase 3A: Success - `ConfirmAsync`

**When:** Ticket purchase persists successfully to database

```csharp
await _inventory.ConfirmAsync(holdId: "abc123...");
```

**What happens:**
```csharp
_holds.TryRemove(holdId, out _);  // Just remove the hold
```

**Why simple?** Inventory was already consumed during `ReserveAsync`. The hold just tracks the temporary state.

**Final state:**
```
_slots["1:3"] = Slot { Available: 98 }  // Stays at 98 (consumed)
_holds["abc123..."] = <removed>          // Hold no longer needed
```

---

### Phase 3B: Failure - `ReleaseAsync`

**When:** 
- Database save fails
- User cancels checkout
- Payment fails

```csharp
await _inventory.ReleaseAsync(holdId: "abc123...");
```

**What happens:**
```csharp
if (_holds.TryRemove(holdId, out var hold) && 
	_slots.TryGetValue(hold.SlotKey, out var slot))
{
	lock (slot.Gate)
	{
		slot.Available += hold.Quantity;  // Return tickets to pool
	}
}
```

**Final state:**
```
_slots["1:3"] = Slot { Available: 100 }  // Back to original (released)
_holds["abc123..."] = <removed>
```

---

### Phase 4: Automatic Cleanup - `ReleaseExpiredHoldsAsync`

**When:** Background service runs every 30 seconds (via `HoldExpiryService`)

```csharp
var released = await _inventory.ReleaseExpiredHoldsAsync();
```

**What happens:**
```csharp
var now = DateTimeOffset.UtcNow;
var expiredIds = _holds
	.Where(kv => kv.Value.ExpiresAt <= now)
	.Select(kv => kv.Key)
	.ToList();

foreach (var holdId in expiredIds)
{
	// Release each expired hold (same as manual ReleaseAsync)
	if (_holds.TryRemove(holdId, out var hold) && 
		_slots.TryGetValue(hold.SlotKey, out var slot))
	{
		lock (slot.Gate)
		{
			slot.Available += hold.Quantity;
		}
		_logger.LogInformation("Released expired hold {HoldId}", holdId);
	}
}
```

**Purpose:** Prevents abandoned shopping carts from locking inventory forever.

---

## Complete Purchase Flow Example

### Scenario: Two users try to buy the last 3 tickets

**Initial State:**
```
_slots["1:3"] = Slot { Available: 3 }
```

### Timeline:

| Time | Request A | Request B | Slot State |
|------|-----------|-----------|------------|
| T0 | `ReserveAsync(qty=2)` called | | Available: 3 |
| T1 | 🔒 Lock acquired | Waiting on lock | Available: 3 |
| T2 | Check: 3 >= 2 ✓ | Still waiting | Available: 3 |
| T3 | Decrement: Available = 1 | Still waiting | Available: 1 |
| T4 | Hold created: "holdA" | Still waiting | Available: 1 |
| T5 | 🔓 Lock released | | Available: 1 |
| T6 | | 🔒 Lock acquired | Available: 1 |
| T7 | | Check: 1 >= 2 ✗ | Available: 1 |
| T8 | | Return: InsufficientInventory | Available: 1 |
| T9 | `ConfirmAsync("holdA")` | | Available: 1 |
| T10 | Hold removed ✓ | Gets error message | Available: 1 |

**Result:** Request A succeeds with 2 tickets, Request B gets "Only 1 ticket available" error. ✅ No overselling!

---

## Usage in TicketsController

### Step 1: Seed Inventory
```csharp
// Before first purchase, seed with remaining capacity
await _inventory.EnsureSeededAsync(
	eventId: request.EventId,
	categoryId: request.CategoryId,
	capacity: maxTicketsForCategory - soldTicketsInCategory
);
```

### Step 2: Reserve Tickets
```csharp
var reservation = await _inventory.ReserveAsync(
	request.EventId, 
	request.CategoryId, 
	request.Quantity
);

if (reservation.Status == ReservationStatus.InsufficientInventory)
	return BadRequest($"Only {reservation.Remaining} tickets available");

var holdId = reservation.HoldId;  // Keep this for later
```

### Step 3: Persist to Database
```csharp
try
{
	var tickets = GenerateTickets(request);
	await _uow.Tickets.CreateTicketsAsync(tickets);

	// Success! Confirm the hold
	await _inventory.ConfirmAsync(holdId);

	return CreatedAtAction(...);
}
catch
{
	// Failure! Release the hold
	await _inventory.ReleaseAsync(holdId);
	throw;
}
```

---

## Configuration

### Hold Duration
Configured in `appsettings.json`:

```json
{
  "Inventory": {
	"HoldDurationSeconds": 600  // 10 minutes default
  }
}
```

### Service Registration (Program.cs)
```csharp
// Singleton so counters are shared across all requests
builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();

// Background service for automatic cleanup
builder.Services.AddHostedService<HoldExpiryService>();
```

---

## Thread Safety Guarantees

### ✅ Safe Operations

1. **Check + Decrement**: Atomic under lock
   ```csharp
   lock (slot.Gate) { if (available >= qty) available -= qty; }
   ```

2. **Concurrent Reads**: `ConcurrentDictionary.TryGetValue` is thread-safe

3. **Multiple Slots**: Different event-category combinations use different locks (no contention)

### ⚠️ Important Notes

- **Per-slot locking**: Only locks the specific category being purchased
- **Fine-grained**: Event 1, Category A doesn't block Event 1, Category B
- **No deadlocks**: Single lock acquisition per operation

---

## Advantages

1. **✅ No Overselling**: Atomic operations guarantee correctness
2. **✅ No External Dependencies**: Pure in-memory, no Redis required
3. **✅ Fast**: Lock-based operations are extremely fast
4. **✅ Scalable per Slot**: Different categories don't block each other
5. **✅ Automatic Cleanup**: Expired holds released automatically
6. **✅ Simple Testing**: Easy to test without mocking external services

## Limitations

1. **❌ Single Server Only**: State is lost on restart (use Redis for production)
2. **❌ No Persistence**: Holds don't survive application crashes
3. **❌ Memory-bound**: Large numbers of concurrent holds consume memory

---

## Production Alternative: Redis Implementation

For production with multiple servers, replace with Redis:

```csharp
public class RedisInventoryService : IInventoryService
{
	public async Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity)
	{
		var key = $"{eventId}:{categoryId}";

		// Atomic decrement in Redis
		var remaining = await _redis.DecrByAsync(key, quantity);

		if (remaining < 0)
		{
			await _redis.IncrByAsync(key, quantity); // Rollback
			return InsufficientInventory;
		}

		var holdId = Guid.NewGuid().ToString();
		await _redis.SetAsync($"hold:{holdId}", ...);
		await _redis.ExpireAsync($"hold:{holdId}", _holdDuration);

		return Reserved;
	}
}
```

---

## Testing Considerations

### Unit Test Example
```csharp
[Fact]
public async Task ReserveAsync_TwoConcurrentRequests_PreventsOverselling()
{
	var inventory = new InMemoryInventoryService(...);
	await inventory.EnsureSeededAsync(1, 1, 5);

	// Simulate concurrent requests
	var task1 = inventory.ReserveAsync(1, 1, 3);
	var task2 = inventory.ReserveAsync(1, 1, 3);

	var results = await Task.WhenAll(task1, task2);

	// One should succeed, one should fail
	Assert.Single(results, r => r.Status == ReservationStatus.Reserved);
	Assert.Single(results, r => r.Status == ReservationStatus.InsufficientInventory);
}
```

---

## Summary

The `InMemoryInventoryService` implements a **hold/reserve/confirm pattern** with:

1. **Atomic Operations**: Check-and-decrement under lock prevents race conditions
2. **Hold Mechanism**: Temporary reservations with configurable expiry
3. **Automatic Cleanup**: Background service releases expired holds
4. **Thread-Safe**: Per-slot locking for optimal concurrency
5. **Simple Integration**: Easy to use with try-catch rollback pattern

This ensures **zero overselling** even under high concurrency without requiring external infrastructure like Redis during development.
