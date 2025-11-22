# 🐳 Docker Containerization Guide for OrderFlow.Core

A simplified guide to containerizing and running the OrderFlow.Core application with Docker.

---

## 📑 Table of Contents

1. [⚡ Quick Start](#-quick-start)
2. [📋 Docker Compose Files Overview](#-docker-compose-files-overview)
3. [💻 Development Setup (Recommended)](#-development-setup-recommended)
4. [🚀 Full Stack Setup](#-full-stack-setup)
5. [🔧 Understanding the Configuration](#-understanding-the-configuration)
6. [⌨️ Common Commands](#️-common-commands)
7. [🔍 Troubleshooting](#-troubleshooting)

---

## ⚡ Quick Start

### 💻 For Local Development (Recommended)

Run only RabbitMQ in Docker, debug your app in Visual Studio:

```bash
# Start RabbitMQ
docker-compose -f docker-compose.dev.yml up -d

# Access RabbitMQ Management UI
# 🌐 http://localhost:15672 (admin/admin123)

# Run your app from Visual Studio (F5)
```

### 🚀 For Full Stack Testing

Run both RabbitMQ and the application in Docker:

```bash
# Start all services
docker-compose up -d

# 🌐 Access Application: http://localhost:8080
# 📖 Access Swagger: http://localhost:8080/swagger
# 🐰 Access RabbitMQ UI: http://localhost:15672
```

---

## 📋 Docker Compose Files Overview

### Two Files, Two Purposes

| File | Purpose | What Runs | When to Use |
|------|---------|-----------|-------------|
| `docker-compose.dev.yml` | 💻 Development | RabbitMQ only | Debugging in Visual Studio |
| `docker-compose.yml` | 🚀 Full Stack | RabbitMQ + App | Testing complete system |

---

## 💻 Development Setup (Recommended)

### 🤔 Why Use docker-compose.dev.yml?

**Best for**: Day-to-day development and debugging

**Benefits**:
- ✅ Run app directly in Visual Studio with full debugging
- ✅ Hot reload and fast code changes
- ✅ RabbitMQ runs in Docker (consistent environment)
- ✅ No need to rebuild Docker images for code changes

### 📄 File: docker-compose.dev.yml

```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: orderflow-rabbitmq-dev
    hostname: rabbitmq-dev
    ports:
      - "5672:5672"    # Message broker port
      - "15672:15672"  # Management UI port
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    volumes:
      - rabbitmq_dev_data:/var/lib/rabbitmq
    networks:
      - orderflow-dev-network

networks:
  orderflow-dev-network:
    driver: bridge

volumes:
  rabbitmq_dev_data:
    name: orderflow_rabbitmq_dev_data
```

### 🔍 What Each Part Does

#### ⚙️ Service Configuration

```yaml
rabbitmq:
  image: rabbitmq:3-management  # Official RabbitMQ with web UI
  container_name: orderflow-rabbitmq-dev  # Easy to identify
  hostname: rabbitmq-dev  # Used for DNS resolution
```

**Key Points**:
- 🐰 Uses official RabbitMQ image with management plugin
- 🏷️ Container gets a friendly name for easy reference
- 🌐 Hostname allows connection via name instead of IP

#### 🔌 Port Mappings

```yaml
ports:
  - "5672:5672"    # Your app connects here
  - "15672:15672"  # Web UI for monitoring
```

**What This Means**:
- 📨 Port 5672: Application sends/receives messages
- 🖥️ Port 15672: Browser access to RabbitMQ dashboard

#### 🔐 Environment Variables

```yaml
environment:
  RABBITMQ_DEFAULT_USER: admin
  RABBITMQ_DEFAULT_PASS: admin123
```

**Why Needed**:
- 🚫 Default `guest/guest` only works from localhost
- 🔑 Custom credentials allow Docker containers to connect
- ⚠️ **Note**: Change password for production!

#### 💚 Health Checks

```yaml
healthcheck:
  test: rabbitmq-diagnostics -q ping
  interval: 10s
  timeout: 5s
  retries: 5
  start_period: 30s
```

**Purpose**:
- ✅ Ensures RabbitMQ is fully ready before app connects
- 🛡️ Prevents "connection refused" errors on startup
- ⏱️ 30-second grace period for initialization

**Health Check Timeline**:
```
0s   → 🟡 Container starts
30s  → 🔵 First health check
40s  → 🔄 Retry if needed
50s  → 🟢 Usually healthy by now
```

#### 💾 Data Persistence

```yaml
volumes:
  - rabbitmq_dev_data:/var/lib/rabbitmq
```

**What Gets Saved**:
- 📦 Queues and exchanges
- 📧 Messages (if persistent)
- 👤 User accounts
- ⚙️ Configuration

**Why Important**:
```bash
# ❌ Without volume
docker-compose down  # All data lost!

# ✅ With volume
docker-compose down  # Data preserved
docker-compose up    # Everything restored
```

#### 🌐 Networking

```yaml
networks:
  - orderflow-dev-network
```

**How It Works**:
- 🔒 Creates isolated network for containers
- 🔍 Enables DNS-based service discovery
- 🏷️ Containers use names instead of IPs

### ⚙️ Configuration in Your App

When running from Visual Studio, your `appsettings.json` should use:

```json
{
  "RabbitMq": {
    "HostName": "localhost",  // ← Use localhost (app not in container)
    "Port": 5672,
    "UserName": "admin",
    "Password": "admin123"
  }
}
```

### 🔄 Development Workflow

```bash
# 1. Start RabbitMQ
docker-compose -f docker-compose.dev.yml up -d

# 2. Verify it's running
docker-compose -f docker-compose.dev.yml ps
# Should show: orderflow-rabbitmq-dev (healthy) ✅

# 3. Check logs if needed
docker-compose -f docker-compose.dev.yml logs -f rabbitmq

# 4. Open Visual Studio and press F5 to debug 🐛

# 5. When done, stop RabbitMQ
docker-compose -f docker-compose.dev.yml down
```

---

## 🚀 Full Stack Setup

### 🤔 Why Use docker-compose.yml?

**Best for**: Integration testing, demos, production-like testing

**Benefits**:
- ✅ Complete system in Docker
- ✅ Test exactly as it will run in production
- ✅ No local dependencies needed
- ✅ Easy to share entire setup

### 📄 File: docker-compose.yml

```yaml
services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: orderflow-rabbitmq
    hostname: rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: admin
      RABBITMQ_DEFAULT_PASS: admin123
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 30s
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    networks:
      - orderflow-network

  orderflow-core:
    build:
      context: .
      dockerfile: Dockerfile
    container_name: orderflow-core
    hostname: orderflow-core
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_HTTP_PORTS=8080
      - ASPNETCORE_URLS=http://+:8080
      - RabbitMq__HostName=rabbitmq  # ← Uses service name
      - RabbitMq__Port=5672
      - RabbitMq__UserName=admin
      - RabbitMq__Password=admin123
      - RabbitMq__ExchangeName=order_exchange
      - RabbitMq__ExchangeType=topic
    depends_on:
      rabbitmq:
        condition: service_healthy
    networks:
      - orderflow-network
    restart: unless-stopped

networks:
  orderflow-network:
    driver: bridge

volumes:
  rabbitmq_data:
    name: orderflow_rabbitmq_data
```

### 🔑 Key Differences from Dev Setup

#### 🏗️ Application Service

```yaml
orderflow-core:
  build:
    context: .
    dockerfile: Dockerfile
```

**What Happens**:
- 🔨 Builds your application from source
- 📦 Creates optimized container image
- 🚀 Runs application inside Docker

#### 🔗 Service Dependencies

```yaml
depends_on:
  rabbitmq:
    condition: service_healthy
```

**Startup Order**:
```
1. 🟡 Start RabbitMQ
2. ⏳ Wait for health check to pass
3. 🟢 Start OrderFlow.Core
4. ✅ App connects successfully
```

#### ⚙️ Environment Variables

```yaml
environment:
  - RabbitMq__HostName=rabbitmq  # ← Different from dev!
```

**Why Different**:
- 🌐 Both services in same Docker network
- 🏷️ Use service name, not `localhost`
- 🔍 Docker DNS resolves `rabbitmq` to correct container

**Double Underscore Syntax**:
```yaml
RabbitMq__HostName=rabbitmq
```
Maps to:
```json
{
  "RabbitMq": {
    "HostName": "rabbitmq"
  }
}
```

#### 🔄 Restart Policy

```yaml
restart: unless-stopped
```

**Behavior**:
- 💥 App crashes → Docker restarts automatically
- 🛑 Manual stop → Stays stopped
- 🔄 Machine reboots → Services start automatically

---

## 🔧 Understanding the Configuration

### 📊 Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                  🐳 Docker Environment                   │
│                                                          │
│  ┌──────────────────┐         ┌──────────────────┐     │
│  │  🐰 RabbitMQ     │         │  🚀 OrderFlow    │     │
│  │                  │         │     Core         │     │
│  │  Port: 5672      │◄────────│   (.NET 8)       │     │
│  │  UI: 15672       │         │  Port: 8080      │     │
│  │  User: admin     │         │  Swagger: /swagger│    │
│  └──────────────────┘         └──────────────────┘     │
│           │                            │                │
│           └────────┬───────────────────┘                │
│                    │                                    │
│          ┌─────────▼─────────┐                         │
│          │  🌐 Docker Network│                         │
│          │  (Bridge Driver)  │                         │
│          └───────────────────┘                         │
│                                                          │
│  ┌───────────────────────────────────────────┐         │
│  │  💾 Volume: rabbitmq_data                 │         │
│  │  (Persistent Storage)                     │         │
│  └───────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────┘
          │                            │
          │                            │
    localhost:15672           localhost:8080
    (🐰 RabbitMQ UI)          (🌐 API/Swagger)
```

### 🔌 Port Mappings Explained

| Service | Container Port | Host Port | Access From |
|---------|----------------|-----------|-------------|
| 🐰 RabbitMQ AMQP | 5672 | 5672 | 📱 Application |
| 🖥️ RabbitMQ UI | 15672 | 15672 | 🌐 Browser |
| 🚀 OrderFlow API | 8080 | 8080 | 🌐 Browser/Postman |

**Port Format**: `"HOST:CONTAINER"`

```yaml
ports:
  - "8080:8080"  # localhost:8080 → container:8080
```

### 🌐 Networking Concepts

#### 🔗 Container-to-Container Communication

```
Inside Docker Network:
  orderflow-core connects to: rabbitmq:5672
  
From Your Computer:
  🌐 Browser connects to: localhost:8080
  💻 App (Visual Studio) connects to: localhost:5672
```

#### 🏷️ Why Use Service Names?

❌ **Bad** (Using IP addresses):
```yaml
RabbitMq__HostName: 172.18.0.2  # Can change on restart!
```

✅ **Good** (Using service names):
```yaml
RabbitMq__HostName: rabbitmq  # Always resolves correctly
```

### 💾 Volume Persistence

#### ❌ What Happens Without Volumes

```bash
1. Start RabbitMQ
2. Create queues and messages
3. docker-compose down
4. Start again
→ 💥 All data LOST!
```

#### ✅ What Happens With Volumes

```bash
1. Start RabbitMQ
2. Create queues and messages
3. docker-compose down
4. Start again
→ 🎉 All data RESTORED!
```

#### 📦 Volume Commands

```bash
# List all volumes
docker volume ls

# Inspect volume details
docker volume inspect orderflow_rabbitmq_data

# Remove volume (deletes data!)
docker volume rm orderflow_rabbitmq_data

# Backup volume
docker run --rm \
  -v orderflow_rabbitmq_data:/data \
  -v $(pwd):/backup \
  alpine tar czf /backup/rabbitmq-backup.tar.gz -C /data .
```

---

## ⌨️ Common Commands

### 💻 Development Workflow

```bash
# Start RabbitMQ only (for development)
docker-compose -f docker-compose.dev.yml up -d

# View logs
docker-compose -f docker-compose.dev.yml logs -f rabbitmq

# Check status
docker-compose -f docker-compose.dev.yml ps

# Stop RabbitMQ
docker-compose -f docker-compose.dev.yml down

# Stop and remove data
docker-compose -f docker-compose.dev.yml down -v
```

### 🚀 Full Stack Commands

```bash
# Start all services
docker-compose up -d

# Start and rebuild
docker-compose up -d --build

# View all logs
docker-compose logs -f

# View specific service logs
docker-compose logs -f orderflow-core

# Check status
docker-compose ps

# Restart a service
docker-compose restart orderflow-core

# Stop all services
docker-compose down

# Stop and remove volumes (data loss!)
docker-compose down -v
```

### 🛠️ Useful Docker Commands

```bash
# Execute command in container
docker-compose exec orderflow-core bash

# View container resource usage
docker-compose stats

# Remove unused images and containers
docker system prune -a

# View container IP address
docker inspect orderflow-core | grep IPAddress
```

---

## 🔍 Troubleshooting

### ❌ Issue 1: Container Won't Start

**Check logs**:
```bash
docker-compose logs orderflow-core
```

**Common causes**:
- 🔴 Port already in use
- 🔴 Build errors
- 🔴 Missing environment variables

**Solution**:
```bash
# Check what's using the port
netstat -ano | findstr :8080

# Rebuild from scratch
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### 🔴 Issue 2: Can't Connect to RabbitMQ

**Verify RabbitMQ is healthy**:
```bash
docker-compose ps
# Should show "healthy" status ✅
```

**Check logs**:
```bash
docker-compose logs rabbitmq
```

**Common causes**:
- ⏳ RabbitMQ still starting (wait 30-60 seconds)
- 🔴 Wrong hostname in configuration
- 🔴 Wrong credentials

**Solution for Visual Studio (dev)**:
```json
{
  "RabbitMq": {
    "HostName": "localhost",  // ← Must be localhost
    "UserName": "admin",
    "Password": "admin123"
  }
}
```

**Solution for Docker (full stack)**:
```yaml
environment:
  - RabbitMq__HostName=rabbitmq  # ← Must be service name
```

### ⚠️ Issue 3: Port Already in Use

**Error**:
```
Error: Bind for 0.0.0.0:8080 failed: port is already allocated
```

**Solutions**:

**Option 1: Change host port**
```yaml
ports:
  - "8081:8080"  # Use different port
```

**Option 2: Find and stop conflicting process**
```bash
# Windows
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# Linux/Mac
lsof -i :8080
kill -9 <PID>
```

### 🔨 Issue 4: Build Fails

**Error**:
```
failed to solve: process "/bin/sh -c dotnet restore" did not complete
```

**Solutions**:

1. **Check internet connection** (NuGet needs to download packages) 🌐

2. **Clear Docker cache**:
```bash
docker-compose build --no-cache
```

3. **Build locally first**:
```bash
dotnet restore
dotnet build
docker-compose build
```

### 💾 Issue 5: Data Lost After Restart

**Cause**: Volumes not configured or removed

**Check volumes**:
```bash
docker volume ls | grep orderflow
```

**Ensure volume is defined**:
```yaml
volumes:
  - rabbitmq_data:/var/lib/rabbitmq

volumes:
  rabbitmq_data:
    name: orderflow_rabbitmq_data
```

**Avoid**:
```bash
# ⚠️ This deletes volumes!
docker-compose down -v
```

### 🐌 Issue 6: Slow Container Startup

**Cause**: Large build context

**Solution**: Create `.dockerignore` file:
```
**/.vs
**/.vscode
**/bin
**/obj
**/node_modules
**/.git
**/publish
```

---

## 📚 Best Practices

### ✅ Do

1. **Use docker-compose.dev.yml for development**
   - 🚀 Faster development cycle
   - 🐛 Better debugging experience

2. **Pin image versions in production**
   ```yaml
   image: rabbitmq:3.12-management
   ```

3. **Use environment variables for configuration**
   - 🚫 No hardcoded values in code
   - ⚙️ Easy to change per environment

4. **Always use health checks for dependencies**
   - 🛡️ Prevents startup errors
   - ✅ Ensures services are ready

5. **Use named volumes for data persistence**
   - 📦 Easy to manage
   - 💾 Survives container restarts

### ❌ Don't

1. **Don't use `docker-compose down -v` unless you want to delete data**

2. **Don't hardcode IPs in configuration**
   ```yaml
   RabbitMq__HostName: 172.18.0.2  # Bad!
   ```

3. **Don't skip health checks**
   ```yaml
   depends_on:
     - rabbitmq  # Unsafe!
   ```

4. **Don't commit sensitive credentials**
   - 🔐 Use environment variables
   - 🔑 Use secrets management

5. **Don't expose unnecessary ports**
   ```yaml
   ports:
     - "5432:5432"  # Only if needed externally
   ```

---

## 📖 Quick Reference

### 🎯 When to Use Each File

| Scenario | File | Command |
|----------|------|---------|
| 💻 Daily development | `docker-compose.dev.yml` | `docker-compose -f docker-compose.dev.yml up -d` |
| 🧪 Integration testing | `docker-compose.yml` | `docker-compose up -d` |
| 🎪 Demo/presentation | `docker-compose.yml` | `docker-compose up -d` |

### ⚙️ Configuration Cheat Sheet

| Environment | HostName | Why |
|-------------|----------|-----|
| 💻 Visual Studio (dev) | `localhost` | App runs on host machine |
| 🐳 Docker (full stack) | `rabbitmq` | App runs in container |
| 🌐 Production | Service name or FQDN | Varies by platform |

### 🌐 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| 🚀 API | http://localhost:8080 | None |
| 📖 Swagger | http://localhost:8080/swagger | None |
| 🐰 RabbitMQ UI | http://localhost:15672 | admin/admin123 |

---

## 📝 Summary

### 🎯 Key Takeaways

1. **Two Setup Options**:
   - 💻 `docker-compose.dev.yml`: RabbitMQ only (best for development)
   - 🚀 `docker-compose.yml`: Full stack (best for testing)

2. **Health Checks Are Critical**:
   - ✅ Ensure services are ready before connections
   - 🛡️ Prevent startup failures

3. **Named Volumes Preserve Data**:
   - 💾 Data survives container restarts
   - 📦 Easy to backup and restore

4. **Service Names for DNS**:
   - 🏷️ Use service names in Docker networks
   - 💻 Use `localhost` when app runs outside Docker

5. **Environment Variables for Configuration**:
   - 🚫 No code changes needed
   - ⚙️ Easy to adapt to different environments

---

## 🔗 Resources

- 📘 [Docker Compose Documentation](https://docs.docker.com/compose/)
- 🐰 [RabbitMQ Docker Hub](https://hub.docker.com/_/rabbitmq)
- 🚀 [ASP.NET Core Docker Documentation](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)
- 🌐 [Docker Networking](https://docs.docker.com/network/)
- 💾 [Docker Volumes](https://docs.docker.com/storage/volumes/)

---

<div align="center">

**🐳 OrderFlow.Core - Containerized with Docker**

*Simple. Scalable. Ready for Development and Production.*

✨ Built with .NET 8 | 🐰 RabbitMQ | 🐳 Docker

</div>
