# Development Instructions & Coding Standards

## Event Management API - Coding Guidelines

This document establishes the coding standards and best practices for the Event Management API project. All code contributions must adhere to these guidelines to maintain consistency, quality, and maintainability.

---

## Table of Contents

1. [SOLID Principles](#solid-principles)
2. [Clean Code Standards](#clean-code-standards)
3. [Project Architecture](#project-architecture)
4. [Naming Conventions](#naming-conventions)
5. [Code Organization](#code-organization)
6. [Error Handling](#error-handling)
7. [Testing Standards](#testing-standards)
8. [Code Review Checklist](#code-review-checklist)
9. [Git Workflow](#git-workflow)
10. [Performance Guidelines](#performance-guidelines)

---

## SOLID Principles

### S - Single Responsibility Principle (SRP)

**Rule:** Each class should have one, and only one, reason to change.

#### ✅ Good Example (Current Implementation)
```csharp
// EventRepository.cs - Only handles Event data access
public class EventRepository : IEventRepository
{
	public async Task<Event?> GetByIdAsync(int id) { }
	public async Task<List<Event>> GetAllAsync() { }
	public async Task AddAsync(Event entity) { }
}

// EventsController.cs - Only handles HTTP requests/responses
public class EventsController : ControllerBase
{
	public async Task<ActionResult> GetEvents() { }
	public async Task<ActionResult> CreateEvent(CreateEventRequest request) { }
}

// InMemoryInventoryService.cs - Only handles inventory management
public class InMemoryInventoryService : IInventoryService
{
	public async Task<ReservationResult> ReserveAsync(...) { }
}
```

#### ❌ Bad Example
```csharp
// DON'T: Controller doing repository work AND business logic
public class EventsController : ControllerBase
{
	public async Task<ActionResult> CreateEvent(CreateEventRequest request)
	{
		// ❌ Direct database access in controller
		var exists = await _context.Events.AnyAsync(e => e.Name == request.Name);

		// ❌ Business logic in controller
		var ticketsPerCategory = request.TotalTicketing / request.TierCategoryIds.Count;

		// ❌ Mixing concerns
		await SendEmailNotification(request.Name);
	}
}
```

#### Action Items
- [ ] Each class should focus on a single responsibility
- [ ] Controllers should only handle HTTP concerns (routing, validation, response formatting)
- [ ] Repositories should only handle data access
- [ ] Services should contain business logic
- [ ] Models should only represent data structures

---

### O - Open/Closed Principle (OCP)

**Rule:** Classes should be open for extension but closed for modification.

#### ✅ Good Example
```csharp
// IInventoryService.cs - Interface allows extension
public interface IInventoryService
{
	Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity);
}

// Can extend with different implementations without modifying existing code
public class InMemoryInventoryService : IInventoryService { }
public class RedisInventoryService : IInventoryService { }
public class SqlInventoryService : IInventoryService { }
```

#### ❌ Bad Example
```csharp
// DON'T: Hard-coded logic that requires modification
public class TicketPriceCalculator
{
	public decimal Calculate(string category)
	{
		if (category == "VIP") return 100m;
		else if (category == "Premium") return 75m;
		else if (category == "Standard") return 50m;
		// ❌ Adding new category requires modifying this class
		return 25m;
	}
}
```

#### ✅ Better Approach
```csharp
public interface IPricingStrategy
{
	decimal CalculatePrice();
}

public class VIPPricingStrategy : IPricingStrategy
{
	public decimal CalculatePrice() => 100m;
}

public class PremiumPricingStrategy : IPricingStrategy
{
	public decimal CalculatePrice() => 75m;
}

public class TicketPriceCalculator
{
	public decimal Calculate(IPricingStrategy strategy) => strategy.CalculatePrice();
}
```

#### Action Items
- [ ] Use interfaces for all service dependencies
- [ ] Prefer composition over inheritance
- [ ] Use strategy pattern for variable behaviors
- [ ] Design for extensibility from the start

---

### L - Liskov Substitution Principle (LSP)

**Rule:** Derived classes must be substitutable for their base classes without altering program correctness.

#### ✅ Good Example
```csharp
public interface IRepository<T> where T : class
{
	Task<T?> GetByIdAsync(int id);
	Task<List<T>> GetAllAsync();
	Task AddAsync(T entity);
}

// All implementations honor the contract
public class EventRepository : IRepository<Event> { }
public class TicketRepository : IRepository<Ticket> { }
public class TierCategoryRepository : IRepository<TierCategory> { }

// Can swap implementations without breaking code
public class EventService
{
	private readonly IRepository<Event> _repository;

	public EventService(IRepository<Event> repository)
	{
		_repository = repository; // ✅ Any implementation works
	}
}
```

#### ❌ Bad Example
```csharp
// DON'T: Implementation violates parent contract
public class CachedEventRepository : IRepository<Event>
{
	public async Task<Event?> GetByIdAsync(int id)
	{
		// ❌ Throws exception instead of returning null
		throw new NotImplementedException("Use GetByIdWithCacheAsync instead");
	}
}
```

#### Action Items
- [ ] Ensure derived classes don't change expected behavior
- [ ] Don't throw `NotImplementedException` in production code
- [ ] Honor interface contracts completely
- [ ] Maintain consistent return types and exceptions

---

### I - Interface Segregation Principle (ISP)

**Rule:** No client should be forced to depend on methods it doesn't use.

#### ✅ Good Example (Current Implementation)
```csharp
// Focused interfaces
public interface IEventRepository
{
	Task<Event?> GetByIdAsync(int id);
	Task<List<Event>> GetAllAsync();
	Task AddAsync(Event entity);
}

public interface ITicketRepository
{
	Task<Ticket?> GetByIdAsync(int id);
	Task CreateTicketsAsync(List<Ticket> tickets);
	Task<int> CountByEventAsync(int eventId);
}
```

#### ❌ Bad Example
```csharp
// DON'T: Fat interface forcing unnecessary implementations
public interface IRepository
{
	// ❌ Not all repositories need all these methods
	Task<T> GetById(int id);
	Task BulkInsert(List<T> items);
	Task<Stream> ExportToCsv();
	Task<byte[]> GeneratePdf();
	Task SendEmail(string recipient);
	Task LogAudit(string action);
}
```

#### ✅ Better Approach
```csharp
// Segregated interfaces
public interface IRepository<T>
{
	Task<T?> GetByIdAsync(int id);
	Task AddAsync(T entity);
}

public interface IBulkRepository<T>
{
	Task BulkInsertAsync(List<T> items);
}

public interface IExportable<T>
{
	Task<Stream> ExportToCsvAsync();
}

// Implement only what you need
public class EventRepository : IRepository<Event>, IExportable<Event> { }
```

#### Action Items
- [ ] Create small, focused interfaces
- [ ] Split large interfaces into cohesive units
- [ ] Don't force clients to implement unused methods
- [ ] Use composition to combine behaviors

---

### D - Dependency Inversion Principle (DIP)

**Rule:** Depend on abstractions, not concretions. High-level modules should not depend on low-level modules.

#### ✅ Good Example (Current Implementation)
```csharp
// ✅ Controller depends on abstraction
public class TicketsController : ControllerBase
{
	private readonly IUnitOfWork _uow;           // ✅ Interface
	private readonly IInventoryService _inventory; // ✅ Interface

	public TicketsController(IUnitOfWork uow, IInventoryService inventory)
	{
		_uow = uow;
		_inventory = inventory;
	}
}

// ✅ Dependency injection in Program.cs
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();
```

#### ❌ Bad Example
```csharp
// DON'T: Direct dependency on concrete class
public class TicketsController : ControllerBase
{
	private readonly EventContext _context;              // ❌ Concrete class
	private readonly InMemoryInventoryService _inventory; // ❌ Concrete class

	public TicketsController()
	{
		_context = new EventContext();           // ❌ Direct instantiation
		_inventory = new InMemoryInventoryService(); // ❌ Direct instantiation
	}
}
```

#### Action Items
- [ ] Always use constructor injection
- [ ] Depend on interfaces, not concrete classes
- [ ] Register dependencies in `Program.cs`
- [ ] Never use `new` for services in controllers/business logic
- [ ] Use `ILogger<T>` instead of `Console.WriteLine`

---

## Clean Code Standards

### 1. Naming Conventions

#### Classes
```csharp
// ✅ PascalCase, descriptive nouns
public class EventRepository { }
public class TicketPurchaseService { }
public class InMemoryInventoryService { }

// ❌ Avoid
public class evt_repo { }        // Wrong casing
public class Manager { }         // Too generic
public class Helper { }          // Meaningless
public class Utils { }           // Anti-pattern
```

#### Methods
```csharp
// ✅ PascalCase, verb phrases
public async Task<Event?> GetByIdAsync(int id) { }
public async Task CreateTicketsAsync(List<Ticket> tickets) { }
public async Task<bool> ValidateInventoryAsync(int eventId) { }

// ❌ Avoid
public async Task get(int id) { }      // Wrong casing
public async Task DoStuff() { }        // Vague
public async Task Process() { }        // Not specific
```

#### Variables
```csharp
// ✅ camelCase, descriptive
var eventRepository = ...;
var totalTicketCount = 100;
var maxTicketsPerCategory = 50;

// ❌ Avoid
var repo = ...;           // Too abbreviated
var temp = ...;           // Generic
var data = ...;           // Meaningless
var x = ...;              // Single letter (except loop counters)
```

#### Constants
```csharp
// ✅ PascalCase or UPPER_SNAKE_CASE for magic numbers
public const int MaxTicketsPerPurchase = 10;
public const int DEFAULT_HOLD_DURATION_SECONDS = 600;

// ❌ Avoid
public const int max = 10;         // Wrong casing
public const int CONSTANT = 10;    // Not descriptive
```

#### Interfaces
```csharp
// ✅ Start with 'I', descriptive
public interface IEventRepository { }
public interface IInventoryService { }
public interface IUnitOfWork { }

// ❌ Avoid
public interface EventRepository { }  // Missing 'I' prefix
public interface IData { }            // Too generic
```

---

### 2. Method Size & Complexity

#### Rule: Methods should be small and do one thing

```csharp
// ✅ Good: Small, focused methods
public async Task<ActionResult> PurchaseTickets(PurchaseTicketRequest request)
{
	var validationResult = await ValidateRequestAsync(request);
	if (!validationResult.IsValid)
		return BadRequest(validationResult.Error);

	var reservation = await ReserveInventoryAsync(request);
	if (!reservation.IsSuccessful)
		return BadRequest(reservation.Error);

	var tickets = await CreateAndSaveTicketsAsync(request, reservation.HoldId);

	return CreatedAtAction(nameof(GetTickets), tickets);
}

private async Task<ValidationResult> ValidateRequestAsync(PurchaseTicketRequest request)
{
	// Single responsibility: validation
}

private async Task<ReservationResult> ReserveInventoryAsync(PurchaseTicketRequest request)
{
	// Single responsibility: reservation
}

// ❌ Bad: Too long, does too much
public async Task<ActionResult> PurchaseTickets(PurchaseTicketRequest request)
{
	// 200+ lines of mixed validation, reservation, database operations...
}
```

#### Metrics
- [ ] **Maximum method length:** 50 lines (ideally 20)
- [ ] **Maximum cyclomatic complexity:** 10
- [ ] **Maximum nesting depth:** 3 levels
- [ ] **Maximum parameters:** 4 (use object for more)

---

### 3. Comments & Documentation

#### When to Comment
```csharp
// ✅ Good: Explain WHY, not WHAT
public async Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity)
{
	// Atomic check-and-decrement prevents race conditions during concurrent purchases
	lock (slot.Gate)
	{
		if (slot.Available < quantity)
			return InsufficientInventory;

		slot.Available -= quantity;
	}
}

// ✅ Good: Public API documentation
/// <summary>
/// Atomically reserves tickets and creates a time-limited hold.
/// This operation is thread-safe and prevents overselling.
/// </summary>
/// <param name="quantity">Number of tickets to reserve</param>
/// <returns>Reservation result with hold ID if successful</returns>
public async Task<ReservationResult> ReserveAsync(int eventId, int categoryId, int quantity)
{
}

// ❌ Bad: Obvious comments
public async Task<Event?> GetByIdAsync(int id)
{
	// Get event by id  ❌ Useless comment
	return await _context.Events.FirstOrDefaultAsync(e => e.Id == id);
}

// ❌ Bad: Commented-out code
public async Task CreateEvent(Event evt)
{
	// var oldCode = DoSomething();  ❌ Remove this
	// ProcessEvent(evt);            ❌ Use Git history instead
	await _context.Events.AddAsync(evt);
}
```

#### Action Items
- [ ] Use XML documentation (`///`) for public APIs
- [ ] Comment complex algorithms or business rules
- [ ] Explain non-obvious decisions
- [ ] Don't state the obvious
- [ ] Remove commented-out code (use Git)
- [ ] Keep comments up-to-date with code

---

### 4. Error Handling

#### ✅ Good Practices
```csharp
// Specific exception handling
public async Task<ActionResult> PurchaseTickets(PurchaseTicketRequest request)
{
	try
	{
		var tickets = await CreateTicketsAsync(request);
		await _inventory.ConfirmAsync(holdId);
		return Ok(tickets);
	}
	catch (DbUpdateException ex)
	{
		// Log and release inventory on database failure
		_logger.LogError(ex, "Failed to persist tickets for event {EventId}", request.EventId);
		await _inventory.ReleaseAsync(holdId);
		return StatusCode(500, "Failed to complete purchase");
	}
	catch (InvalidOperationException ex)
	{
		// Handle known business rule violations
		_logger.LogWarning(ex, "Invalid purchase attempt for event {EventId}", request.EventId);
		return BadRequest(ex.Message);
	}
}

// ❌ Bad Practices
public async Task<ActionResult> PurchaseTickets(PurchaseTicketRequest request)
{
	try
	{
		// ...
	}
	catch (Exception ex)  // ❌ Catching all exceptions
	{
		// ❌ Swallowing exception
		return Ok();      // ❌ Hiding the error
	}
}
```

#### Rules
- [ ] Catch specific exceptions, not `Exception`
- [ ] Always log exceptions with context
- [ ] Don't swallow exceptions silently
- [ ] Use try-finally for cleanup (or `using` statements)
- [ ] Return appropriate HTTP status codes
- [ ] Include meaningful error messages

---

### 5. Async/Await Best Practices

```csharp
// ✅ Good: Proper async all the way
public async Task<ActionResult> GetEvents()
{
	var events = await _repository.GetAllAsync();
	return Ok(events);
}

public async Task<List<Event>> GetAllAsync()
{
	return await _context.Events
		.Include(e => e.Tier)
		.ToListAsync();
}

// ❌ Bad: Blocking async code
public ActionResult GetEvents()
{
	var events = _repository.GetAllAsync().Result;  // ❌ Deadlock risk
	return Ok(events);
}

public async Task<List<Event>> GetAllAsync()
{
	return _context.Events.ToList();  // ❌ Should be ToListAsync()
}
```

#### Rules
- [ ] Use `async`/`await` for all I/O operations
- [ ] Never use `.Result` or `.Wait()`
- [ ] Always await database calls (`ToListAsync`, `FirstOrDefaultAsync`)
- [ ] Suffix async methods with `Async`
- [ ] Use `Task<T>` for return types, not `void` (except event handlers)

---

### 6. LINQ & Query Optimization

```csharp
// ✅ Good: Efficient queries
public async Task<List<Event>> GetUpcomingEventsAsync()
{
	return await _context.Events
		.Where(e => e.Date >= DateTime.UtcNow)
		.OrderBy(e => e.Date)
		.Include(e => e.Tier)
		.ThenInclude(t => t.TierCategories)
		.AsNoTracking()  // ✅ Read-only query
		.ToListAsync();
}

// ❌ Bad: Inefficient queries
public async Task<List<Event>> GetUpcomingEventsAsync()
{
	var allEvents = await _context.Events.ToListAsync();  // ❌ Loads all data
	return allEvents
		.Where(e => e.Date >= DateTime.UtcNow)  // ❌ Filters in-memory
		.OrderBy(e => e.Date)
		.ToList();
}

// ❌ Bad: N+1 query problem
public async Task<List<EventResponse>> GetEventsAsync()
{
	var events = await _context.Events.ToListAsync();
	foreach (var evt in events)
	{
		evt.Tier = await _context.Tiers.FindAsync(evt.TierId);  // ❌ N queries
	}
	return events;
}
```

#### Rules
- [ ] Use `.AsNoTracking()` for read-only queries
- [ ] Use `.Include()` to avoid N+1 queries
- [ ] Filter in database with `.Where()`, not in-memory
- [ ] Use `.Select()` to project only needed columns
- [ ] Avoid loading entire tables

---

## Project Architecture

### Folder Structure

```
EventManagementApi2/
├── Controllers/          # HTTP endpoints (thin layer)
│   ├── EventsController.cs
│   └── TicketsController.cs
│
├── Models/              # Domain models & DTOs
│   ├── Event.cs
│   ├── Ticket.cs
│   ├── CreateEventRequest.cs
│   └── EventResponse.cs
│
├── Data/                # Data access layer
│   ├── EventContext.cs
│   ├── IUnitOfWork.cs
│   ├── UnitOfWork.cs
│   └── Repositories/
│       ├── IEventRepository.cs
│       └── EventRepository.cs
│
├── Services/            # Business logic
│   ├── IInventoryService.cs
│   ├── InMemoryInventoryService.cs
│   └── HoldExpiryService.cs
│
└── Program.cs           # DI configuration
```

### Layer Responsibilities

| Layer | Responsibility | Should NOT |
|-------|---------------|------------|
| **Controllers** | HTTP routing, request/response | Business logic, data access |
| **Services** | Business logic, orchestration | HTTP concerns, direct DB access |
| **Repositories** | Data access, queries | Business rules, HTTP concerns |
| **Models** | Data structures | Methods (except simple helpers) |

---

## Code Review Checklist

### Before Submitting PR

- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Added tests for new functionality
- [ ] Follows SOLID principles
- [ ] Methods are small (<50 lines)
- [ ] No commented-out code
- [ ] No hardcoded values (use configuration)
- [ ] Proper error handling and logging
- [ ] Async/await used correctly
- [ ] No N+1 query problems
- [ ] Proper null checks
- [ ] Meaningful variable/method names
- [ ] XML documentation for public APIs
- [ ] Updated README if needed

### Reviewer Checklist

- [ ] Code follows project conventions
- [ ] SOLID principles applied
- [ ] No code duplication
- [ ] Thread-safety considered
- [ ] Performance implications reviewed
- [ ] Security concerns addressed
- [ ] Dependencies properly injected
- [ ] Error paths tested
- [ ] Edge cases handled

---

## Git Workflow

### Branch Naming
```
feature/add-payment-integration
bugfix/fix-inventory-race-condition
hotfix/critical-security-patch
refactor/improve-repository-pattern
docs/update-api-documentation
```

### Commit Messages
```
✅ Good:
feat: Add ticket cancellation endpoint
fix: Prevent overselling in concurrent purchases
refactor: Extract validation logic to separate service
docs: Update README with Docker instructions

❌ Bad:
update
fix bug
changes
WIP
```

### Pull Request Guidelines
- Keep PRs small (<400 lines)
- Include description of changes
- Reference related issues
- Update documentation
- Add screenshots for UI changes
- Request review from at least one team member

---

## Performance Guidelines

### Database
- [ ] Use `.AsNoTracking()` for read-only queries
- [ ] Add indexes on frequently queried columns
- [ ] Use pagination for large result sets
- [ ] Avoid `SELECT *`, project specific columns
- [ ] Use batch operations for bulk inserts

### Memory
- [ ] Dispose `IDisposable` resources (use `using` statements)
- [ ] Don't hold large objects in memory unnecessarily
- [ ] Use `IAsyncEnumerable` for streaming large datasets
- [ ] Consider caching for frequently accessed data

### Concurrency
- [ ] Use locks for shared mutable state
- [ ] Prefer `ConcurrentDictionary` over `Dictionary` + locks
- [ ] Test concurrent scenarios
- [ ] Use `async`/`await` to avoid blocking threads

---

## Testing Standards

### Test Organization
```csharp
// Arrange-Act-Assert pattern
[Fact]
public async Task ReserveAsync_WithSufficientInventory_ReturnsReserved()
{
	// Arrange
	var inventory = new InMemoryInventoryService(...);
	await inventory.EnsureSeededAsync(eventId: 1, categoryId: 1, capacity: 10);

	// Act
	var result = await inventory.ReserveAsync(1, 1, quantity: 5);

	// Assert
	Assert.Equal(ReservationStatus.Reserved, result.Status);
	Assert.NotNull(result.HoldId);
	Assert.Equal(5, result.Remaining);
}
```

### Test Naming
```
[Method]_[Scenario]_[ExpectedBehavior]

Examples:
- ReserveAsync_WithInsufficientInventory_ReturnsInsufficientInventory
- CreateEvent_WithInvalidTierId_ReturnsBadRequest
- GetEvents_WithNoEvents_ReturnsEmptyList
```

### Coverage Goals
- [ ] Minimum 80% code coverage
- [ ] 100% coverage for critical paths (inventory, payment)
- [ ] Test happy paths and error cases
- [ ] Test edge cases (boundaries, nulls, empty lists)

---

## Additional Resources

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/best-practices)
- [Entity Framework Core Performance](https://docs.microsoft.com/en-us/ef/core/performance/)
- [Clean Code by Robert C. Martin](https://www.amazon.com/Clean-Code-Handbook-Software-Craftsmanship/dp/0132350882)

---

## Enforcement

These guidelines are **mandatory** for all code contributions. Pull requests that don't follow these standards will be rejected with feedback for improvement.

### Automated Checks
- Code analysis with StyleCop/Roslyn analyzers
- Build warnings treated as errors
- Minimum test coverage enforcement
- Dependency scanning for vulnerabilities

### Manual Review
- All PRs require at least one approval
- Senior developers review architecture changes
- Security-critical code requires security review

---

## Questions?

If you have questions about these guidelines or need clarification, please:
1. Check existing code for examples
2. Ask in team discussions
3. Propose improvements via pull request to this document

---

**Last Updated:** 2025-01-27  
**Version:** 1.0  
**Maintained By:** Development Team

---

## Quick Reference Card

### SOLID in One Line Each
- **S**: One class, one job
- **O**: Extend behavior, don't modify code
- **L**: Substitutable without breaking
- **I**: Small, focused interfaces
- **D**: Depend on abstractions

### Clean Code Checklist
✅ Meaningful names  
✅ Small methods  
✅ One level of abstraction  
✅ DRY (Don't Repeat Yourself)  
✅ Proper error handling  
✅ Async all the way  
✅ Comments explain WHY  
✅ Tests for everything  

### Remember
> "Any fool can write code that a computer can understand. Good programmers write code that humans can understand." - Martin Fowler
