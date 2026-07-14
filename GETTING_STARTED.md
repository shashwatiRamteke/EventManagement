# 🚀 Getting Started with Ticket Purchase API

## 5-Minute Quick Start

### 1. **Check Everything is Working**
```powershell
# Build the solution
dotnet build
```

Expected: ✅ Build successful

### 2. **Run the Application**
```powershell
# From EventManagementApi2 directory
dotnet run
```

Expected: App starts, listening on port 5000

### 3. **Test a Simple Request**
```powershell
# In another PowerShell window
Invoke-WebRequest -Uri "http://localhost:5000/api/events" -UseBasicParsing
```

Expected: ✅ HTTP 200 with events data

---

## Purchasing Your First Ticket

### Step 1: Purchase Tickets
```powershell
$ticketRequest = @{
	eventId = 4
	categoryId = 1
	quantity = 1
	buyerName = "Your Name"
	buyerEmail = "your@email.com"
	buyerPhone = "+1-555-0100"
} | ConvertTo-Json

$response = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/purchase" `
  -Method POST `
  -Headers @{"Content-Type" = "application/json"} `
  -Body $ticketRequest `
  -UseBasicParsing

$purchase = $response.Content | ConvertFrom-Json
Write-Host "✅ Purchased ticket(s): $($purchase.tickets.Count)"
Write-Host "Total Price: \$$($purchase.totalPrice)"

# Save ticket ID for next steps
$ticketId = $purchase.tickets[0].id
Write-Host "Ticket ID: $ticketId"
```

Expected Output:
```
✅ Purchased ticket(s): 1
Total Price: $50
Ticket ID: 1
```

---

### Step 2: View Your Tickets
```powershell
$email = "your@email.com"
$myTickets = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/buyer/$email" `
  -UseBasicParsing | ConvertFrom-Json

Write-Host "Your Tickets:"
$myTickets.tickets | ForEach-Object {
	Write-Host "  • $($_.ticketNumber) - $($_.eventName) - \$$($_.price)"
}
```

Expected Output:
```
Your Tickets:
  • TKT-0004-001-1231456789-5678 - Health & Wellness Expo - $50
```

---

### Step 3: Check A Specific Ticket
```powershell
$ticket = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/$ticketId" `
  -UseBasicParsing | ConvertFrom-Json

Write-Host "Ticket Details:"
Write-Host "  Event: $($ticket.eventName)"
Write-Host "  Category: $($ticket.categoryName)"
Write-Host "  Buyer: $($ticket.buyerName) ($($ticket.buyerEmail))"
Write-Host "  Status: $($ticket.status)"
Write-Host "  Price: \$$($ticket.price)"
```

Expected Output:
```
Ticket Details:
  Event: Health & Wellness Expo
  Category: VIP
  Buyer: Your Name (your@email.com)
  Status: Active
  Price: $50
```

---

### Step 4: Use Your Ticket
```powershell
# At the event, ticket is scanned and marked as used
$used = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/$ticketId/mark-used" `
  -Method PATCH `
  -UseBasicParsing | ConvertFrom-Json

Write-Host "✅ Ticket status: $($used.status)"
```

Expected Output:
```
✅ Ticket status: Used
```

---

### Step 5: Request Refund
```powershell
# Create a new ticket first to cancel
$request = @{
	eventId = 4
	categoryId = 1
	quantity = 1
	buyerName = "Test"
	buyerEmail = "test@example.com"
} | ConvertTo-Json

$resp = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/purchase" `
  -Method POST `
  -Headers @{"Content-Type" = "application/json"} `
  -Body $request `
  -UseBasicParsing

$newTicketId = ($resp.Content | ConvertFrom-Json).tickets[0].id

# Cancel for refund
$cancelled = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/$newTicketId/cancel" `
  -Method PATCH `
  -UseBasicParsing | ConvertFrom-Json

Write-Host "✅ Ticket cancelled"
Write-Host "Refund Amount: \$$($cancelled.price)"
```

Expected Output:
```
✅ Ticket cancelled
Refund Amount: $50
```

---

### Step 6: View Event Statistics
```powershell
$stats = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/statistics/event/4" `
  -UseBasicParsing | ConvertFrom-Json

Write-Host "Event Statistics:"
Write-Host "  Event: $($stats.eventName)"
Write-Host "  Available: $($stats.totalTicketsAvailable)"
Write-Host "  Sold: $($stats.totalTicketsSold)"
Write-Host "  Active: $($stats.totalTicketsActive)"
Write-Host "  Used: $($stats.totalTicketsUsed)"
Write-Host "  Cancelled: $($stats.totalTicketsCancelled)"
Write-Host "  Revenue: \$$($stats.totalRevenue)"
Write-Host "  Remaining: $($stats.availableTickets)"
```

Expected Output:
```
Event Statistics:
  Event: Health & Wellness Expo
  Available: 800
  Sold: 2
  Active: 1
  Used: 1
  Cancelled: 1
  Revenue: $50
  Remaining: 798
```

---

## Complete Workflow Script

Save this as `test-tickets.ps1` and run it:

```powershell
#!/usr/bin/env pwsh

Write-Host "=== TICKET API WORKFLOW TEST ===" -ForegroundColor Cyan

# 1. Purchase ticket
Write-Host "`n1. Purchasing ticket..." -ForegroundColor Yellow
$purchase = @{
	eventId = 4
	categoryId = 1
	quantity = 1
	buyerName = "Alice"
	buyerEmail = "alice@example.com"
} | ConvertTo-Json

$resp1 = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/purchase" `
  -Method POST `
  -Headers @{"Content-Type" = "application/json"} `
  -Body $purchase `
  -UseBasicParsing
$ticket = ($resp1.Content | ConvertFrom-Json).tickets[0]
$ticketId = $ticket.id
Write-Host "✅ Purchased: $($ticket.ticketNumber)" -ForegroundColor Green

# 2. Get tickets by email
Write-Host "`n2. Retrieving tickets by email..." -ForegroundColor Yellow
$resp2 = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/buyer/alice@example.com" `
  -UseBasicParsing
$tickets = $resp2.Content | ConvertFrom-Json
Write-Host "✅ Found: $($tickets.total) ticket(s)" -ForegroundColor Green

# 3. Get ticket detail
Write-Host "`n3. Getting ticket details..." -ForegroundColor Yellow
$resp3 = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/$ticketId" `
  -UseBasicParsing
$detail = $resp3.Content | ConvertFrom-Json
Write-Host "✅ Ticket: $($detail.ticketNumber) - Status: $($detail.status)" -ForegroundColor Green

# 4. Mark as used
Write-Host "`n4. Marking ticket as used..." -ForegroundColor Yellow
$resp4 = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/$ticketId/mark-used" `
  -Method PATCH `
  -UseBasicParsing
$used = $resp4.Content | ConvertFrom-Json
Write-Host "✅ Ticket status: $($used.status)" -ForegroundColor Green

# 5. Get statistics
Write-Host "`n5. Getting event statistics..." -ForegroundColor Yellow
$resp5 = Invoke-WebRequest -Uri "http://localhost:5000/api/tickets/statistics/event/4" `
  -UseBasicParsing
$stats = $resp5.Content | ConvertFrom-Json
Write-Host "✅ Event: $($stats.eventName)" -ForegroundColor Green
Write-Host "   Sold: $($stats.totalTicketsSold) | Used: $($stats.totalTicketsUsed) | Revenue: \$$($stats.totalRevenue)" -ForegroundColor Green

Write-Host "`n=== TEST COMPLETE ===" -ForegroundColor Cyan
```

Run it:
```powershell
powershell -ExecutionPolicy Bypass -File test-tickets.ps1
```

---

## Common API Responses

### Successful Purchase (201 Created)
```json
{
  "success": true,
  "message": "Successfully purchased 2 ticket(s)",
  "tickets": [...],
  "totalPrice": 100.00
}
```

### Ticket Details (200 OK)
```json
{
  "id": 1,
  "ticketNumber": "TKT-0004-001-1231456789-5678",
  "eventName": "Health & Wellness Expo",
  "categoryName": "VIP",
  "buyerName": "John Doe",
  "buyerEmail": "john@example.com",
  "price": 50.00,
  "status": "Active",
  "purchaseDate": "2026-07-15T10:30:00Z"
}
```

### Error Response (400 Bad Request)
```json
{
  "error": "Only 798 tickets available for this category"
}
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| API not responding | Make sure app is running: `dotnet run` |
| 404 on event | Check event ID exists: `GET /api/events` |
| Invalid email | Use valid format: `user@domain.com` |
| Quantity error | Max 10 per request, or purchase multiple times |
| 500 error | Check app logs for details |

---

## Next Steps

1. **Read full documentation:** `TICKET_API_DOCUMENTATION.md`
2. **Review all test cases:** `TICKET_API_TESTING.md`
3. **Quick reference:** `TICKET_API_QUICK_REFERENCE.md`
4. **Implementation notes:** `TICKET_IMPLEMENTATION_SUMMARY.md`

---

**Status:** ✅ Ready to Use  
**Framework:** .NET 10  
**Base URL:** http://localhost:5000/api/tickets
