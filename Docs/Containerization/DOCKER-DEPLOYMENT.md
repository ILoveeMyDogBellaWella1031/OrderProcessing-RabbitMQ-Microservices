# OrderFlow.Core - Docker Deployment Guide

## Overview
OrderFlow.Core is a .NET 8 microservice application that uses RabbitMQ for message-based communication. This guide explains how to deploy the application using Docker Compose.

## Prerequisites
- Docker Desktop installed and running
- .NET 8 SDK (for local builds)

## Architecture
The application consists of two main services:
- **orderflow-core**: .NET 8 Web API with Swagger UI
- **rabbitmq**: RabbitMQ message broker with management UI

Both services communicate through a custom Docker network (`orderflow-network`).

## Deployment Options

This guide covers **three deployment approaches**:

1. **[Full Docker Deployment](#quick-start)** - Both app and RabbitMQ in Docker (Production-like)
2. **[Hybrid Development](#hybrid-development-visual-studio--docker)** - Visual Studio for app + Docker for RabbitMQ (Best for development)
3. **Local Development** - Everything runs locally (Covered in main README)

Choose the approach that best fits your workflow.

---

## Quick Start

### 1. Build the Application Locally
First, publish the .NET application:
```bash
dotnet publish -c Release -o ./publish
```

### 2. Start Services with Docker Compose
```bash
docker-compose up -d
```

This will:
- Start RabbitMQ and wait for it to be healthy
- Start the OrderFlow.Core application
- Create a bridge network for inter-service communication

### 3. Verify Services are Running
```bash
docker-compose ps
```

You should see both containers running:
- `orderflow-rabbitmq`: Status `healthy`
- `orderflow-core`: Status `Up`

### 4. Access the Application

#### Swagger UI (API Documentation)
- **URL**: http://localhost:8080/swagger
- Provides interactive API documentation and testing interface

#### Health Check
- **URL**: http://localhost:8080/health
- Returns JSON with application health status and RabbitMQ connectivity

#### RabbitMQ Management UI
- **URL**: http://localhost:15672
- **Username**: `admin`
- **Password**: `admin123`
- Manage queues, exchanges, and monitor message flow

---

## Hybrid Development: Visual Studio + Docker

**Best for active development** - Debug in Visual Studio while RabbitMQ runs in Docker.

⚠️ **Important:** For hybrid development, run the app **directly from Visual Studio** (not in a container). Use the **"http"** or **"https"** profile, NOT "Container (Dockerfile)".

### Quick Setup

**1. Start RabbitMQ Only**
```powershell
docker-compose -f docker-compose.dev.yml up -d
```

**2. Configure Local Connection**

Ensure `appsettings.Development.json` uses `localhost`:
```json
{
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "admin",
    "Password": "admin123",
    "ExchangeName": "order_exchange",
    "ExchangeType": "topic"
  }
}
```

**3. Select Correct Visual Studio Profile**

In Visual Studio, select **"https"** or **"http"** profile from the dropdown (NOT "Container (Dockerfile)"):

![Visual Studio Profile Selection](https://via.placeholder.com/500x100/2563eb/ffffff?text=Select+%22https%22+or+%22http%22+profile)

**Available Profiles in `launchSettings.json`:**

| Profile | Use For | RabbitMQ Location |
|---------|---------|-------------------|
| **http** ✅ | Hybrid development | `localhost:5672` |
| **https** ✅ | Hybrid development (with SSL) | `localhost:5672` |
| IIS Express | IIS testing | `localhost:5672` |
| Container (Dockerfile) ❌ | Full Docker (not hybrid) | `host.docker.internal` |

**Why NOT "Container (Dockerfile)"?**
- ❌ Runs app **inside Docker** (loses Visual Studio debugging benefits)
- ❌ Requires image rebuild for code changes (slow)
- ❌ No Hot Reload support
- ❌ Limited breakpoint functionality
- ✅ Use this profile **only** for testing full Docker deployment

**4. Run from Visual Studio**
- Select **"http"** or **"https"** profile from the toolbar dropdown
- Press **F5** (with debugger) or **Ctrl+F5** (without debugger)
- Set breakpoints in controllers and subscribers
- Use Hot Reload for fast iteration

**5. Verify Connection**
- RabbitMQ UI: http://localhost:15672
- App Health (http profile): http://localhost:5246/health
- App Health (https profile): https://localhost:7279/health
- Swagger (http profile): http://localhost:5246/swagger
- Swagger (https profile): https://localhost:7279/swagger

### Benefits
- ✅ Full Visual Studio debugger with breakpoints
- ✅ Hot Reload for instant code changes
- ✅ Production-like RabbitMQ in Docker
- ✅ No containerization overhead during development

### Common Commands
```powershell
# Start RabbitMQ
docker-compose -f docker-compose.dev.yml up -d

# Check status
docker-compose -f docker-compose.dev.yml ps

# View logs
docker-compose -f docker-compose.dev.yml logs -f

# Stop RabbitMQ
docker-compose -f docker-compose.dev.yml down
```

### Profile Comparison

**Hybrid Development (Recommended):**
```json
// Use "http" or "https" profile from launchSettings.json
{
  "profiles": {
    "https": {
      "commandName": "Project",  // ← Runs directly from Visual Studio
      "applicationUrl": "https://localhost:7279;http://localhost:5246",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Full Docker (Not for hybrid development):**
```json
// DON'T use "Container (Dockerfile)" for hybrid development
{
  "profiles": {
    "Container (Dockerfile)": {
      "commandName": "Docker",  // ← Runs in Docker container
      "environmentVariables": {
        "RabbitMq__HostName": "host.docker.internal"  // Different hostname
      }
    }
  }
}
```

### Troubleshooting

**Cannot connect to RabbitMQ:**
1. Verify RabbitMQ is running: `docker ps`
2. Check you're using **"http"** or **"https"** profile (NOT "Container (Dockerfile)")
3. Check `appsettings.Development.json` has `"HostName": "localhost"`
4. Test connection: `Test-NetConnection localhost -Port 5672`
   - Test command should show `TcpTestSucceeded : True`

**Using wrong Visual Studio profile:**
```
Error: BrokerUnreachableException when using "Container (Dockerfile)" profile
```

**Solution:**
1. Click the profile dropdown in Visual Studio toolbar
2. Select **"https"** or **"http"** (not "Container (Dockerfile)")
3. Press F5 to run

**Port conflict:**
```powershell
# Find process using port
netstat -ano | findstr :7279

# Kill the process or change port in launchSettings.json
```

### When to Use Each Approach

| Scenario | Approach | Visual Studio Profile |
|----------|----------|----------------------|
| **Active development** | Hybrid (VS + Docker RabbitMQ) | **http** or **https** |
| **Testing Docker deployment** | Full Docker | Container (Dockerfile) |
| **Production build testing** | Full Docker Compose | N/A (use docker-compose) |
| **Debugging subscribers** | Hybrid | **http** or **https** |

---

## Quick Start

### 1. Build the Application Locally
First, publish the .NET application:
```bash
dotnet publish -c Release -o ./publish
```

### 2. Start Services with Docker Compose
```bash
docker-compose up -d
```

This will:
- Start RabbitMQ and wait for it to be healthy
- Start the OrderFlow.Core application
- Create a bridge network for inter-service communication

### 3. Verify Services are Running
```bash
docker-compose ps
```

You should see both containers running:
- `orderflow-rabbitmq`: Status `healthy`
- `orderflow-core`: Status `Up`

### 4. Access the Application

#### Swagger UI (API Documentation)
- **URL**: http://localhost:8080/swagger
- Provides interactive API documentation and testing interface

#### Health Check
- **URL**: http://localhost:8080/health
- Returns JSON with application health status and RabbitMQ connectivity

#### RabbitMQ Management UI
- **URL**: http://localhost:15672
- **Username**: `admin`
- **Password**: `admin123`
- Manage queues, exchanges, and monitor message flow

---

## Application Services

### API Endpoints
The application includes an Orders API with the following endpoints:
- `POST /api/orders` - Create a new order
- `GET /api/orders/{id}` - Get order by ID

### Message Subscribers
The application automatically subscribes to the following RabbitMQ queues:
1. **order_processing_queue** - Processes new orders
2. **payment_verification_queue** - Handles payment verification
3. **shipping_queue** - Manages shipping notifications
4. **notification_queue** - Sends customer notifications

## Configuration

### Environment Variables
The following environment variables are configured in `docker-compose.yml`:

#### Application Settings
- `ASPNETCORE_ENVIRONMENT`: Set to `Development`
- `ASPNETCORE_URLS`: `http://+:8080`

#### RabbitMQ Connection
- `RabbitMq__HostName`: `rabbitmq` (Docker service name)
- `RabbitMq__Port`: `5672`
- `RabbitMq__UserName`: `admin`
- `RabbitMq__Password`: `admin123`
- `RabbitMq__ExchangeName`: `order_exchange`
- `RabbitMq__ExchangeType`: `topic`

### Port Mappings
- **8080**: Application HTTP API
- **5672**: RabbitMQ AMQP protocol
- **15672**: RabbitMQ Management UI

## Docker Commands

### View Logs
```bash
# All services
docker-compose logs

# Specific service
docker-compose logs orderflow-core
docker-compose logs rabbitmq

# Follow logs in real-time
docker-compose logs -f orderflow-core
```

### Stop Services
```bash
docker-compose down
```

### Stop and Remove Volumes
```bash
docker-compose down -v
```

### Rebuild and Restart
```bash
# Rebuild application locally
dotnet publish -c Release -o ./publish

# Rebuild Docker image
docker-compose build

# Restart services
docker-compose up -d
```

## Troubleshooting

### Container Won't Start
1. Check logs: `docker-compose logs orderflow-core`
2. Verify RabbitMQ is healthy: `docker-compose ps`
3. Ensure port 8080 is not in use: `netstat -ano | findstr :8080` (Windows)

### Cannot Connect to RabbitMQ
1. Check RabbitMQ health: `docker-compose ps`
2. Wait for RabbitMQ to be fully started (usually 10-15 seconds)
3. Check RabbitMQ logs: `docker-compose logs rabbitmq`

### Swagger UI Not Loading
1. Verify container is running: `docker-compose ps`
2. Check application logs: `docker-compose logs orderflow-core`
3. Test health endpoint: `curl http://localhost:8080/health`
4. Ensure you're accessing `http://localhost:8080/swagger` (not HTTPS)

### NuGet Restore Issues During Docker Build

**Error Message:**
```
error NU1301: Unable to load the service index for source https://api.nuget.org/v3/index.json.
Failed to restore /src/OrderFlow.Core.csproj
```

**Cause:**
This error occurs when Docker cannot reach NuGet.org during the `docker-compose build` process. Common causes include:
- Network connectivity issues within Docker
- Firewall/proxy blocking Docker's external connections
- DNS resolution problems in Docker Desktop
- Corporate network restrictions

**Solution (Recommended):**

Use the **simplified build approach** that builds locally first:

**Step 1: Build Application Locally**
```bash
# PowerShell
dotnet publish -c Release -o ./publish

# This builds the app on your machine where NuGet connectivity works
```

**Step 2: Verify docker-compose.yml Uses Simplified Dockerfile**
```yaml
orderflow-core:
  build:
    context: .
    dockerfile: Dockerfile.simple  # ✅ Should use Dockerfile.simple
```

**Step 3: Build and Start Services**
```bash
docker-compose build
docker-compose up -d
```

**Why This Works:**
- ✅ Builds locally where NuGet connectivity is stable
- ✅ Docker only copies pre-built binaries (no NuGet needed)
- ✅ Faster builds (no dependency downloads in Docker)
- ✅ Avoids all NuGet connectivity issues

**Alternative Solution (Advanced):**

If you prefer using the multi-stage `Dockerfile` (not recommended for local development):

**Option A: Configure Docker DNS**
```yaml
# Add to docker-compose.yml under orderflow-core service
orderflow-core:
  dns:
    - 8.8.8.8
    - 8.8.4.4
```

**Option B: Use Host Network (Windows/Linux)**
```yaml
orderflow-core:
  network_mode: "host"
```

**Option C: Configure NuGet Retry in Dockerfile**
```dockerfile
# Add after FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
ENV NUGET_XMLDOC_MODE=skip

# Clear and retry
RUN dotnet nuget locals all --clear
RUN dotnet restore "./OrderFlow.Core.csproj" --disable-parallel
```

**Complete Troubleshooting Steps:**
These steps combine the solutions above into a clean, comprehensive process to resolve NuGet connectivity issues during Docker builds.

```powershell
# Step 1: Clean everything
docker-compose down -v
Remove-Item -Recurse -Force ./publish -ErrorAction SilentlyContinue

# Step 2: Build locally
dotnet clean
dotnet publish -c Release -o ./publish

# Step 3: Verify Dockerfile.simple exists and is correct
Get-Content Dockerfile.simple

# Step 4: Rebuild Docker images
docker-compose build --no-cache

# Step 5: Start services
docker-compose up -d

# Step 6: Verify
docker-compose ps
docker-compose logs orderflow-core
```

**Expected Output:**
```
✓ Container orderflow-rabbitmq   Healthy
✓ Container orderflow-core       Started
```

**If Still Failing:**

1. **Check Docker Desktop Network Settings:**
   - Docker Desktop → Settings → Resources → Network
   - Ensure "Use kernel networking" is enabled (Windows)

2. **Check Firewall:**
   ```powershell
   # Windows: Allow Docker through firewall
   New-NetFirewallRule -DisplayName "Docker" -Direction Inbound -Action Allow
   ```

3. **Test Docker Internet Connectivity:**
   ```bash
   # Should return "Hello from Docker!"
   docker run hello-world
   ```

4. **Check Docker Logs:**
   ```powershell
   # Windows
   Get-EventLog -LogName Application -Source Docker

   # Or check Docker Desktop logs
   # %LOCALAPPDATA%\Docker\log.txt
   ```

5. **Reset Docker Desktop (Last Resort):**
   - Docker Desktop → Troubleshoot → Reset to factory defaults
   - Restart Docker Desktop
   - Try build again

**Quick Reference:**

| Issue | Solution |
|-------|----------|
| NU1301 error | Build locally first (`dotnet publish -c Release -o ./publish`) |
| Slow restore | Use `Dockerfile.simple` (already configured) |
| Proxy issues | Configure Docker Desktop proxy settings |
| DNS issues | Add `dns: [8.8.8.8, 8.8.4.4]` to docker-compose.yml |
| Firewall block | Allow Docker through firewall |

**Best Practice:**
Always use the **simplified approach** for local development:
```bash
dotnet publish -c Release -o ./publish && docker-compose up -d --build
```

This ensures consistent, fast, and reliable builds without NuGet connectivity issues.

## Network Architecture

```
┌─────────────────────────────────────────────┐
│         orderflow-network (bridge)          │
│                                             │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │   RabbitMQ   │◄────►│ OrderFlow.Core  │ │
│  │  :5672       │      │   :8080         │ │
│  │  :15672      │      │                 │ │
│  └──────────────┘      └─────────────────┘ │
│         ▲                       ▲           │
└─────────┼───────────────────────┼───────────┘
          │                       │
          │                       │
     localhost:15672         localhost:8080
     (Management UI)         (Swagger UI)
```

## Production Considerations

For production deployment, consider:
1. **Use secrets management** for RabbitMQ credentials (Azure Key Vault, Docker secrets)
2. **Enable HTTPS** with proper certificates
3. **Configure persistent volumes** for RabbitMQ data
4. **Set resource limits** in docker-compose.yml
5. **Use environment-specific configuration** files
6. **Enable monitoring and logging** (Application Insights, ELK stack)
7. **Implement health checks** at the infrastructure level (Kubernetes, Azure Container Apps)

## Files

- `docker-compose.yml`: Docker Compose orchestration configuration
- `Dockerfile.simple`: Simplified Dockerfile for containerization
- `Dockerfile`: Original multi-stage Dockerfile (for reference)
- `appsettings.json`: Application configuration
- `Program.cs`: Application startup and service registration

## Support

For issues or questions:
1. Check application logs: `docker-compose logs orderflow-core`
2. Verify RabbitMQ connectivity: `http://localhost:15672`
3. Test health endpoint: `http://localhost:8080/health`
