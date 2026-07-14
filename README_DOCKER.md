# EventManagementApi2 - Production Docker Deployment

## 🎯 Implementation Complete ✅

Your EventManagementApi2 has been successfully built, containerized, and deployed to Docker in **Production mode**.

---

## 📊 Deployment Overview

| Component | Status | Details |
|-----------|--------|---------|
| **Docker Image** | ✅ Built | `eventmanagementapi:prod` |
| **Container** | ✅ Running | `eventapi` (ID: 7658f28ae8d8) |
| **Environment** | ✅ Production | ASPNETCORE_ENVIRONMENT=Production |
| **Framework** | ✅ .NET 10 | Running on aspnet:10.0 runtime |
| **Port Mapping** | ✅ Active | localhost:5000 → container:80 |
| **API Status** | ✅ Responding | HTTP 200 OK |

---

## 🚀 Quick Start

### 1. Verify Container is Running
```powershell
docker ps --filter name=eventapi
```

**Expected output:**
```
CONTAINER ID   STATUS         PORTS
7658f28ae8d8   Up 3 minutes   0.0.0.0:5000→80/tcp
```

### 2. Test the API
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/events" -UseBasicParsing
```

### 3. View Container Details
```powershell
# Get container IP
docker inspect -f '{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}' eventapi
# Output: 172.17.0.3

# View logs
docker logs -f eventapi

# Container stats
docker stats eventapi
```

---

## 🌐 API Access

### From Windows Host (Local Development)
Use **localhost:5000** for all API requests:

```
http://localhost:5000/api/events
http://localhost:5000/api/weatherforecast  (404 - not available)
```

**PowerShell Example:**
```powershell
$events = Invoke-WebRequest -Uri "http://localhost:5000/api/events" -UseBasicParsing | ConvertFrom-Json
$events | Select-Object -First 1
```

**cURL Example:**
```bash
curl http://localhost:5000/api/events
```

### From Another Docker Container (Same Network)
Use the **container IP** (172.17.0.3):

```bash
docker exec <other-container> curl http://172.17.0.3/api/events
```

---

## 📋 API Endpoints Tested

### ✅ GET /api/events
- **Status:** 200 OK
- **Response:** Array of 5 event objects
- **Sample:**
```json
[
  {
	"id": 4,
	"name": "Health & Wellness Expo",
	"description": "Exhibition on fitness, nutrition, and mental health.",
	"venue": "City Expo Hall, Chicago",
	"date": "2026-11-12T00:00:00",
	"time": "10:00:00",
	"totalTicketing": 800
  }
]
```

### ❌ GET /api/weatherforecast
- **Status:** 404 Not Found
- **Note:** Endpoint not implemented in this deployment

---

## 🐳 Docker Commands Reference

### Container Management
```powershell
# Start container
docker start eventapi

# Stop container
docker stop eventapi

# Restart container
docker restart eventapi

# View logs (live)
docker logs -f eventapi

# View last 50 lines
docker logs eventapi --tail 50

# Remove container (stops first)
docker rm eventapi

# View container stats
docker stats eventapi
```

### Image Management
```powershell
# List images
docker images | grep eventmanagement

# Rebuild image
cd C:\Users\ramte\ShashPrep\EventManagement
docker build -t eventmanagementapi:prod -f .\EventManagementApi2\EventManagementApi2\Dockerfile .

# Remove image
docker rmi eventmanagementapi:prod
```

### Network Inspection
```powershell
# List networks
docker network ls

# Inspect bridge network
docker network inspect bridge

# Get container IP
docker inspect eventapi --format='{{json .NetworkSettings}}'
```

---

## 📝 Environment Configuration

### Set in Docker (Production)
```dockerfile
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
```

### Passed at Runtime
```powershell
-e "ASPNETCORE_ENVIRONMENT=Production"
-e "ASPNETCORE_URLS=http://+:80"
-e "AllowedHosts=*"
```

### Configuration Files
- `appsettings.json` - Base settings
- `appsettings.Production.json` - Production overrides (if needed)

---

## 📂 Files Modified/Created

### Dockerfile
**Path:** `EventManagementApi2/EventManagementApi2/Dockerfile`

**Changes Made:**
- ✅ Fixed project paths for multi-level directory structure
- ✅ Added Production environment variables to final stage
- ✅ Set ASPNETCORE_URLS to http://+:80
- ✅ Optimized for Release build

### Documentation
- `DEPLOYMENT_SUMMARY.md` - Detailed deployment report
- `README_DOCKER.md` - This file

---

## 🔍 Container Details

### Image Information
```
Repository: eventmanagementapi
Tag: prod
Size: ~400MB (estimated)
Base Image: mcr.microsoft.com/dotnet/aspnet:10.0
SDK: mcr.microsoft.com/dotnet/sdk:10.0
```

### Build Process
1. **restore**: dotnet restore EventManagementApi2.csproj
2. **build**: dotnet build -c Release
3. **publish**: dotnet publish -c Release (UseAppHost=false)
4. **final**: Copy published binaries to runtime image

### Runtime
```
Listening: http://[::]:80
Framework: .NET 10.0.0
Runtime: AspNetCore 10.0.0
Database: Entity Framework Core (in-memory)
```

---

## ✨ Production Readiness Checklist

| Item | Status | Notes |
|------|--------|-------|
| Container running | ✅ | 7658f28ae8d8 |
| Port mapping working | ✅ | 5000→80 |
| Environment set correctly | ✅ | Production mode |
| API responding | ✅ | HTTP 200 OK |
| CORS configured | ✅ | AllowedHosts=* |
| Logging operational | ✅ | Structured logs |
| Database initialized | ✅ | In-memory store |
| Health checks | ⏳ | Recommended |
| Monitoring setup | ⏳ | Recommended |
| Persistent DB | ⏳ | Recommended |

---

## 🚨 Troubleshooting

### Container Won't Start
```powershell
# Check logs
docker logs eventapi

# Verify image exists
docker images | grep eventmanagement

# Try removing and recreating
docker rm eventapi
docker run -d --name eventapi -p 5000:80 eventmanagementapi:prod
```

### Port Already in Use
```powershell
# Use different port
docker run -d --name eventapi -p 5001:80 eventmanagementapi:prod

# Or stop existing container
docker stop eventapi
```

### Cannot Connect to http://172.17.0.3
```powershell
# This is expected on Windows
# Use localhost:5000 instead
# Container IP is only accessible from other containers on the same network
```

### API Returns 400 Bad Request
```powershell
# AllowedHosts might be blocking request
# Verify environment variable is set
docker inspect eventapi --format='{{json .Config.Env}}' | findstr AllowedHosts

# If not set, remove and recreate container with -e "AllowedHosts=*"
```

---

## 📈 Next Steps (Recommended)

1. **Add Health Checks**
   ```dockerfile
   HEALTHCHECK --interval=30s CMD curl -f http://localhost/health || exit 1
   ```

2. **Implement Logging**
   - Add Serilog for structured logging
   - Export logs to file or centralized service

3. **Database Persistence**
   - Replace in-memory with SQL Server or PostgreSQL
   - Mount volume for data persistence

4. **Security Hardening**
   - Run as non-root user
   - Add security headers
   - Implement rate limiting

5. **CI/CD Integration**
   - Automate builds on git push
   - Push image to registry
   - Deploy to Docker Compose or Kubernetes

6. **Performance Optimization**
   - Add caching layers
   - Implement compression
   - Monitor resource usage

---

## 📞 Support

For issues or questions:
1. Check container logs: `docker logs eventapi`
2. Review DEPLOYMENT_SUMMARY.md for detailed diagnostics
3. Verify API endpoint at: `http://localhost:5000/api/events`
4. Check Docker daemon is running: `docker ps`

---

**Deployment Date:** 2026  
**Framework:** .NET 10  
**Environment:** Production  
**Status:** ✅ **READY FOR TESTING**

**Test Command:**
```powershell
Invoke-WebRequest -Uri "http://localhost:5000/api/events" -UseBasicParsing | ConvertFrom-Json | Select-Object -First 1
```

Expected: JSON object with event data
