# Debian 13 Production Server Preparation Guide — School Management System

> **Target OS:** Debian GNU/Linux 13 "Trixie" (64-bit)
> **Deployment Model:** Docker-based, single-host production server
> **Network:** Internal LAN (no public internet domain)
> **Capacity Planning:** 100+ students and future growth

---

## Table of Contents

1. [System Overview and Architecture](#1-system-overview-and-architecture)
2. [Hardware Requirements](#2-hardware-requirements)
3. [LAN DNS Architecture (Omada)](#3-lan-dns-architecture-omada)
4. [Debian 13 Installation and Initial Configuration](#4-debian-13-installation-and-initial-configuration)
5. [Production Service Account](#5-production-service-account)
6. [Install and Configure Docker](#6-install-and-configure-docker)
7. [Docker Daemon Configuration](#7-docker-daemon-configuration)
8. [Firewall Configuration](#8-firewall-configuration)
9. [Directory Structure](#9-directory-structure)
10. [Git and Application Deployment Preparation](#10-git-and-application-deployment-preparation)
11. [Environment Configuration](#11-environment-configuration)
12. [Database Preparation](#12-database-preparation)
13. [Initial System Administrator Account](#13-initial-system-administrator-account)
14. [Reverse Proxy and Application Access](#14-reverse-proxy-and-application-access)
15. [TLS for LAN Deployment](#15-tls-for-lan-deployment)
16. [Monitoring and Alerting](#16-monitoring-and-alerting)
17. [Backup and Disaster Recovery Preparation](#17-backup-and-disaster-recovery-preparation)
18. [System Security Hardening](#18-system-security-hardening)
19. [System Resource and Kernel Configuration](#19-system-resource-and-kernel-configuration)
20. [Verification Checklist](#20-verification-checklist)
21. [Deployment Readiness Test](#21-deployment-readiness-test)
22. [Troubleshooting](#22-troubleshooting)

---

## 1. System Overview and Architecture

### 1.1 Application Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| Backend API | ASP.NET Core (.NET) | 9.0 |
| Frontend | React (Vite) | As defined in `frontend/sms-web/package.json` |
| Database | PostgreSQL | 16 (Alpine) |
| Reverse Proxy | Nginx | 1.27 (Alpine) |
| Monitoring | Prometheus | 2.54.1 |
| Dashboards | Grafana | 11.2.0 |
| Alerting | Alertmanager | 0.27.0 |
| Host Metrics | Node Exporter | 1.8.2 |
| Database Metrics | Postgres Exporter | 0.15.0 |
| Container Metrics | cAdvisor | 0.49.1 |
| Backup | Custom backup container | Docker-based |

### 1.2 Deployment Architecture

The School Management System runs entirely in Docker containers on a single production server. The architecture follows this pattern:

```
Internet/LAN Client
        |
        v
   [Nginx Reverse Proxy]  (ports 80/443, terminates TLS)
        |
        +---> [Frontend Container]  (React SPA, port 3000:80)
        |
        +---> [API Container]  (ASP.NET Core, port 5000:80, HTTP only)
        |
        +---> [PostgreSQL]  (port 5433:5432, mapped externally)
        |
        +---> [Backup Container]  (automated pg_dump)
        |
        +---> [Monitoring Stack]
                |
                +---> [Prometheus]  (port 9090)
                +---> [Grafana]  (port 3001)
                +---> [Alertmanager]  (port 9093)
                +---> [Node Exporter]  (port 9100)
                +---> [Postgres Exporter]  (port 9187)
                +---> [cAdvisor]  (port 8080)
```

### 1.3 Key Design Decisions

- **API runs HTTP-only** inside the container. Nginx terminates TLS and proxies requests to the API.
- **PostgreSQL port 5433 on host** maps to 5432 inside the container.
- **Redis is optional** — only required if `RedisTokenRevocation:ConnectionString` is configured for production token revocation. By default, the system uses in-memory token revocation.
- **Email is disabled** — SMTP functionality has been removed. Password resets are admin-mediated.
- **Swagger is disabled** in production by default (`Swagger__Enabled=false`).

---

## 2. Hardware Requirements

### 2.1 Minimum Specifications

| Resource | Minimum | Recommended |
|----------|---------|-------------|
| CPU | 4 cores | 8 cores (x86-64) |
| RAM | 8 GB | 16 GB |
| Storage | 100 GB | 250 GB SSD/NVMe |
| Storage Type | SSD | NVMe SSD |
| Network | 1 Gbps | 1 Gbps |
| Swap | 2 GB | 4 GB |

### 2.2 What These Resources Support

The minimum specification supports approximately:
- **100–200 students** concurrently
- All application features (enrollments, grades, certificates, accommodation)
- Full monitoring stack
- Daily backups with 30-day retention

The recommended specification supports approximately:
- **500+ students** concurrently
- Future growth and additional modules
- Extended monitoring data retention
- Multiple concurrent backup generations

### 2.3 Storage Considerations

- **Docker images and containers** require approximately 5–10 GB.
- **PostgreSQL data** grows with usage. Estimate 1–5 MB per student per academic year.
- **Uploaded files** (documents, images) require additional space — estimate 50–100 MB per student.
- **Prometheus metrics** approximately 1–2 GB per month.
- **Grafana data** approximately 100 MB.
- **Docker logs** approximately 5–10 GB (configured with log rotation).
- **Backups** require space equal to 1–2x the database size multiplied by retention days.

### 2.4 Network Interface

The server requires a **static private IP address** on the organization's LAN. Configure this during Debian installation or immediately after.

---

## 3. LAN DNS Architecture (Omada)

### 3.1 Overview

The School Management System does **not** use a public internet domain. It uses the **native LAN DNS feature** in the TP-Link Omada gateway/router to map a local hostname to the server's private IP address.

### 3.2 Choosing the Local Hostname

Select a hostname following this convention:

```
school.example.lan
```

Replace `example` with your organization's name or abbreviation. Examples:

- `school.acme.lan`
- `sms.organization.lan`
- `portal.school.lan`

**Rules:**
- Use only lowercase letters, numbers, periods, and hyphens.
- The `.lan` top-level domain is reserved for local networks and will not conflict with public DNS.
- Keep the hostname short and memorable.

### 3.3 Creating the LAN DNS Record in Omada

1. Log in to the **Omada Controller** web interface.
2. Navigate to **Settings > Network > LAN DNS** (or **Services > DNS** depending on firmware version).
3. Click **Add DNS Entry**.
4. Configure:

   | Field | Value |
   |-------|-------|
   | Hostname | `school.example.lan` (or your chosen hostname) |
   | IP Address | The static private IP of the Debian server (e.g., `192.168.1.100`) |
   | Type | `A` record |

5. Click **Save** and **Apply** the configuration.

### 3.4 Verifying DHCP Clients Use the Omada Gateway as DNS

Omada gateway acts as the DHCP server. Ensure that DHCP clients receive the gateway's IP address as their DNS server:

1. In Omada Controller, go to **Settings > Network > LAN**.
2. Verify that **DHCP Server** is enabled and the **Primary DNS** field is either blank (clients use gateway) or set to the gateway's IP address.
3. **Do not** set a public DNS server like 8.8.8.8 as the primary DNS — this would break LAN DNS resolution.

### 3.5 Testing DNS Resolution

#### From Windows

```cmd
nslookup school.example.lan
```

**Expected output:**
```
Name:    school.example.lan
Address:  192.168.1.100
```

#### From Linux

```bash
nslookup school.example.lan
```

or

```bash
dig school.example.lan +short
```

**Expected output:**
```
192.168.1.100
```

#### From Browser

1. Open a web browser on any client on the LAN.
2. Navigate to `http://school.example.lan` (before HTTPS configuration) or `https://school.example.lan` (after HTTPS configuration).
3. If the application is running, the login page should appear.

### 3.6 Troubleshooting DNS Resolution

| Symptom | Cause | Solution |
|---------|-------|----------|
| `nslookup` returns `server failed` | Client is not using Omada gateway as DNS | Check client DHCP settings. Run `ipconfig /all` (Windows) or `ip addr` (Linux) and verify DNS server IP. |
| `nslookup` returns public IP | LAN DNS entry not applied | Wait 1–2 minutes for DNS propagation. Re-apply Omada configuration. |
| `nslookup` returns `Non-existent domain` | Hostname typed incorrectly | Verify the exact hostname in Omada DNS configuration. |
| Browser cannot reach the server | Firewall blocking | Verify firewall rules on the Debian server (see Section 8). |

---

## 4. Debian 13 Installation and Initial Configuration

### 4.1 Installing Debian 13

1. Download the **Debian 13 "Trixie" netinstall ISO** from the official Debian website.
2. Create a bootable USB drive using tools like Rufus (Windows) or `dd` (Linux).
3. Boot the server from the USB drive.
4. Follow the installation wizard:
   - **Language:** English (or your preference)
   - **Location:** Your country
   - **Keyboard layout:** Your keyboard layout
   - **Hostname:** `sms-server` (or your preferred hostname, e.g., `school`)
   - **Domain name:** Leave blank
   - **Root password:** Set a strong root password
   - **Full name for new user:** Create a non-root user (e.g., `admin`)
   - **Partitioning:** Guided - use entire disk with LVM (or manual if you have specific requirements)
   - **Software selection:** Uncheck **Debian desktop environment** and **GNOME**. Only select **SSH server** and **standard system utilities**.
   - **Install GRUB:** Yes

### 4.2 First Boot and Initial Setup

Log in as root or your non-root user with sudo access.

### 4.3 Update APT Repositories and Upgrade

```bash
sudo apt update
sudo apt upgrade -y
sudo apt full-upgrade -y
```

### 4.4 Install Required System Packages

```bash
sudo apt install -y \
    curl \
    wget \
    git \
    vim \
    nano \
    unzip \
    zip \
    gnupg \
    ca-certificates \
    apt-transport-https \
    software-properties-common \
    ufw \
    fail2ban \
    htop \
    nload \
    net-tools \
    dnsutils \
    iproute2 \
    iptables \
    openssl \
    lsb-release \
    chrony \
    logrotate \
    rsync \
    bzip2 \
    gzip \
    tar \
    jq \
    tree
```

### 4.5 Time Zone Configuration

```bash
sudo timedatectl set-timezone Africa/Nairobi
```

Verify:

```bash
timedatectl
```

Expected output shows correct time zone and `NTP service: active`.

### 4.6 NTP / System Time Synchronization

```bash
sudo systemctl enable chrony --now
sudo chronyc sources -v
```

Verify:

```bash
chronyc tracking
```

The output should show `Stratum` and `RMS offset` values indicating synchronization.

### 4.7 Hostname Configuration

```bash
sudo hostnamectl set-hostname sms-server
```

Verify:

```bash
hostnamectl
```

### 4.8 /etc/hosts Configuration

Edit `/etc/hosts`:

```bash
sudo nano /etc/hosts
```

Ensure the file contains:

```
127.0.0.1       localhost
192.168.1.100   sms-server.example.lan sms-server

# The following lines are desirable for IPv6 capable hosts
::1     localhost ip6-localhost ip6-loopback
ff02::1 ip6-allnodes
ff02::2 ip6-allrouters
```

Replace `192.168.1.100` with the actual static IP of the server.

### 4.9 DNS Configuration

Edit `/etc/resolv.conf`:

```bash
sudo nano /etc/resolv.conf
```

Ensure it contains:

```
nameserver 192.168.1.1   # Replace with Omada gateway IP
search example.lan
```

**Note:** On Debian 13, if `systemd-resolved` is running, manage DNS via:

```bash
sudo nano /etc/systemd/resolved.conf
```

Set:

```
[Resolve]
DNS=192.168.1.1
Domains=example.lan
```

Then restart:

```bash
sudo systemctl restart systemd-resolved
```

### 4.10 Static IP Configuration

If you did not configure a static IP during installation, configure it now.

List network interfaces:

```bash
ip addr
```

Identify the interface name (e.g., `eth0` or `ens18`).

Edit the network configuration (Debian 13 uses `systemd-networkd` or `/etc/network/interfaces`).

#### Using /etc/network/interfaces:

```bash
sudo nano /etc/network/interfaces
```

```
auto eth0
iface eth0 inet static
    address 192.168.1.100/24
    gateway 192.168.1.1
    dns-nameservers 192.168.1.1
```

Replace `eth0`, `192.168.1.100`, and `192.168.1.1` with your actual interface and IP addresses.

Restart networking:

```bash
sudo systemctl restart networking
```

### 4.11 Network Verification

```bash
ip addr show
ping -c 4 192.168.1.1   # Ping gateway
ping -c 4 8.8.8.8       # Ping internet (optional, verify outbound)
```

### 4.12 Verify Essential Utilities

```bash
git --version
curl --version
wget --version
vim --version
unzip --version
```

---

## 5. Production Service Account

### 5.1 Create the sms_admin Account

The application requires a dedicated Linux service account to own the application files and directories.

```bash
sudo groupadd --system sms_admin 2>/dev/null || true
sudo useradd --system --gid sms_admin --create-home --home-dir /opt/sms --shell /bin/bash sms_admin 2>/dev/null || echo "User sms_admin already exists"
```

**Explanation:**
- `--system`: Creates a system account (no password expiry, no login shell by default).
- `--gid sms_admin`: Assigns to the `sms_admin` group.
- `--create-home`: Creates the home directory at `/opt/sms`.
- `--shell /bin/bash`: Allows shell access for maintenance.
- `2>/dev/null || true`: Suppresses error if group already exists.
- `2>/dev/null || echo "...":` Handles the case where the user already exists.

**If the user already exists**, verify:

```bash
id sms_admin
```

Expected output: `uid=999(sms_admin) gid=999(sms_admin) groups=999(sms_admin)`

### 5.2 Create the Production Directory Structure

```bash
sudo mkdir -p /opt/sms/{docker,backups,certs,logs,scripts,data}
sudo chown -R sms_admin:sms_admin /opt/sms
sudo chmod 750 /opt/sms
```

### 5.3 Add sms_admin to the Docker Group

```bash
sudo usermod -aG docker sms_admin
```

**⚠️ SECURITY NOTE:** Adding a user to the `docker` group gives them effective root access (they can run any container, mount any filesystem, etc.). Only trusted administrators should be in this group. The `sms_admin` account is a system service account, not a user account, so this risk is acceptable.

### 5.4 Verify the Account

```bash
id sms_admin
groups sms_admin
ls -la /opt/sms
```

---

## 6. Install and Configure Docker

### 6.1 Remove Conflicting Packages

```bash
for pkg in docker.io docker-doc docker-compose podman-docker containerd runc; do
    sudo apt remove -y $pkg 2>/dev/null || true
done
```

### 6.2 Install Docker Prerequisites

```bash
sudo apt update
sudo apt install -y ca-certificates curl gnupg
```

### 6.3 Add Docker's Official GPG Key

```bash
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/debian/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
```

### 6.4 Add the Docker Repository for Debian 13

Debian 13 "Trixie" is not yet released, so we use the Debian testing/unstable repository. Docker does not yet have a Trixie-specific repository, but the **bookworm** (Debian 12) repository works on Trixie at this time.

```bash
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/debian bookworm stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
```

**Note:** When Debian 13 is released and Docker provides a Trixie repository, replace `bookworm` with `trixie` above.

### 6.5 Install Docker Engine

```bash
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

### 6.6 Enable and Start Docker

```bash
sudo systemctl enable docker
sudo systemctl start docker
```

### 6.7 Verify Docker Installation

```bash
docker --version
sudo docker run hello-world
```

**Expected output for `docker --version`:** `Docker version 27.x.x, build xxxxx`

**Expected output for `hello-world`:** A message confirming Docker is working correctly.

### 6.8 Verify Docker Compose

```bash
docker compose version
```

**Expected output:** `Docker Compose version v2.x.x`

### 6.9 Verify Docker Service Status

```bash
sudo systemctl status docker
```

**Expected output:** `Active: active (running)`

### 6.10 Test Docker with sms_admin

```bash
su - sms_admin -c "docker --version"
su - sms_admin -c "docker compose version"
```

### 6.11 Troubleshooting Docker Installation

| Problem | Check | Solution |
|---------|-------|----------|
| `docker: command not found` | Docker package not installed | Run `sudo apt install -y docker-ce` |
| `Cannot connect to Docker daemon` | Docker service not running | Run `sudo systemctl start docker` |
| `permission denied` | User not in docker group | Run `sudo usermod -aG docker $USER && newgrp docker` |
| `apt update` fails for Docker repository | Incorrect repository | Verify the content of `/etc/apt/sources.list.d/docker.list` |

---

## 7. Docker Daemon Configuration

### 7.1 Determine if daemon.json is Required

The School Management System does not require specific Docker daemon configuration for normal operation. The default Docker daemon settings are sufficient.

However, for a production server, consider the following recommended configuration:

```bash
sudo mkdir -p /etc/docker
sudo nano /etc/docker/daemon.json
```

### 7.2 Recommended Configuration

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "100m",
    "max-file": "5"
  },
  "storage-driver": "overlay2",
  "storage-opts": [
    "overlay2.override_kernel_check=true"
  ],
  "live-restore": true,
  "iptables": true,
  "ip-forward": true
}
```

**Explanation:**
- `log-driver` / `log-opts`: Limits container log size to prevent disk exhaustion.
- `storage-driver`: Uses `overlay2` (default and recommended).
- `live-restore`: Keeps containers running if Docker daemon restarts.
- `iptables` / `ip-forward`: Required for container networking.

### 7.3 Validate the JSON Configuration

Before restarting Docker, validate the JSON syntax:

```bash
python3 -m json.tool /etc/docker/daemon.json
```

If this command exits with no output or shows the formatted JSON, the syntax is valid.

### 7.4 Restart Docker

```bash
sudo systemctl restart docker
```

### 7.5 Verify Docker is Running

```bash
sudo systemctl status docker
docker info
```

### 7.6 Recovery from Invalid daemon.json

If Docker fails to start after editing `daemon.json`:

```bash
# Check Docker logs
sudo journalctl -u docker -n 50

# Temporarily remove the invalid file
sudo mv /etc/docker/daemon.json /etc/docker/daemon.json.bak
sudo systemctl restart docker

# Fix the configuration and restore
sudo nano /etc/docker/daemon.json
# (fix the JSON)
python3 -m json.tool /etc/docker/daemon.json
sudo systemctl restart docker
```

---

## 8. Firewall Configuration

### 8.1 Enable UFW (Uncomplicated Firewall)

```bash
sudo ufw --force enable
```

### 8.2 Default Deny Policy

```bash
sudo ufw default deny incoming
sudo ufw default allow outgoing
```

### 8.3 Allow SSH

```bash
sudo ufw allow ssh
```

or explicitly:

```bash
sudo ufw allow 22/tcp
```

### 8.4 Allow HTTP and HTTPS

```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

### 8.5 Allow Grafana (Management Access Only)

If you need to access Grafana from the LAN:

```bash
sudo ufw allow from 192.168.1.0/24 to any port 3001 proto tcp
```

**⚠️ Do not expose Grafana to the entire internet.** Restrict to your LAN subnet.

### 8.6 Allow Prometheus (Administrator Access Only)

If you need to access Prometheus directly:

```bash
sudo ufw allow from 192.168.1.0/24 to any port 9090 proto tcp
```

### 8.7 Port Reference Table

| Port | Service | Accessible From | Purpose |
|------|---------|----------------|---------|
| 22/tcp | SSH | Administrators only | Remote administration |
| 80/tcp | HTTP | All LAN clients | Application access (redirects to HTTPS) |
| 443/tcp | HTTPS | All LAN clients | Application access |
| 5433/tcp | PostgreSQL | 🔒 Do NOT expose externally | Database (Docker internal only) |
| 5000/tcp | API | 🔒 Do NOT expose externally | API (Nginx proxied only) |
| 3000/tcp | Frontend | 🔒 Do NOT expose externally | Frontend (Nginx proxied only) |
| 3001/tcp | Grafana | Administrators only | Monitoring dashboards |
| 9090/tcp | Prometheus | Administrators only | Metrics |
| 9093/tcp | Alertmanager | Administrators only | Alert management |
| 9100/tcp | Node Exporter | 🔒 Do NOT expose externally | Host metrics (Docker internal only) |
| 9187/tcp | Postgres Exporter | 🔒 Do NOT expose externally | Database metrics (Docker internal only) |
| 8080/tcp | cAdvisor | 🔒 Do NOT expose externally | Container metrics (Docker internal only) |

### 8.8 Verify Firewall Rules

```bash
sudo ufw status verbose
```

**Expected output:**

```
Status: active
Logging: on (low)
Default: deny (incoming), allow (outgoing), disabled (routed)
New profiles: skip

To                         Action      From
--                         ------      ----
22/tcp                     ALLOW IN    Anywhere
80/tcp                     ALLOW IN    Anywhere
443/tcp                    ALLOW IN    Anywhere
3001/tcp                   ALLOW IN    192.168.1.0/24
9090/tcp                   ALLOW IN    192.168.1.0/24
22/tcp (v6)                ALLOW IN    Anywhere (v6)
80/tcp (v6)                ALLOW IN    Anywhere (v6)
443/tcp (v6)               ALLOW IN    Anywhere (v6)
```

---

## 9. Directory Structure

### 9.1 Production Directory Layout

The following directory structure is used for the production deployment:

```
/opt/sms/
├── docker/                    # Docker Compose files and configuration
│   ├── docker-compose.yml     # Base compose file
│   ├── docker-compose.prod.yml  # Production override
│   ├── nginx.conf             # Nginx configuration
│   ├── prometheus.yml         # Prometheus configuration
│   ├── prometheus-alerts.yml  # Alert rules
│   ├── alertmanager.yml       # Alertmanager configuration
│   ├── grafana-dashboards/    # Pre-provisioned dashboards
│   ├── grafana-datasources/   # Pre-provisioned datasources
│   └── init-db.sql            # Database initialization script
├── env/                       # Environment files (symlink or copy .env here)
│   └── .env                   # Production environment variables
├── backups/                   # Database backups (Docker volume)
├── certs/                     # TLS certificates
│   ├── school.example.lan.crt
│   └── school.example.lan.key
├── logs/                      # Application logs (Docker volume)
├── scripts/                   # Deployment and maintenance scripts
│   ├── deploy.sh
│   ├── backup.sh
│   ├── restore.sh
│   ├── seed.sh
│   └── migrate.sh
├── data/                      # Persistent application data
│   └── uploads/               # Uploaded files
└── .git/                      # Git repository (cloned from GitHub)
```

### 9.2 Ownership and Permissions

| Path | Owner | Group | Permissions |
|------|-------|-------|-------------|
| `/opt/sms/` | sms_admin | sms_admin | 750 |
| `/opt/sms/docker/` | sms_admin | sms_admin | 750 |
| `/opt/sms/env/` | sms_admin | sms_admin | 750 |
| `/opt/sms/env/.env` | sms_admin | sms_admin | 600 |
| `/opt/sms/backups/` | sms_admin | sms_admin | 750 |
| `/opt/sms/certs/` | sms_admin | sms_admin | 750 |
| `/opt/sms/certs/*.key` | sms_admin | sms_admin | 600 |
| `/opt/sms/certs/*.crt` | sms_admin | sms_admin | 644 |
| `/opt/sms/logs/` | sms_admin | sms_admin | 750 |
| `/opt/sms/scripts/` | sms_admin | sms_admin | 750 |
| `/opt/sms/scripts/*.sh` | sms_admin | sms_admin | 750 |
| `/opt/sms/data/` | sms_admin | sms_admin | 750 |
| `/opt/sms/data/uploads/` | sms_admin | sms_admin | 750 |

---

## 10. Git and Application Deployment Preparation

### 10.1 Repository Information

The project is hosted at: `https://github.com/hudson254/SchoolManagementSystem.git`

### 10.2 Clone the Repository

```bash
cd /opt/sms
sudo -u sms_admin git clone https://github.com/hudson254/SchoolManagementSystem.git /opt/sms
```

### 10.3 Authentication for Private Repository

If the repository is private, use one of the following methods:

#### Option A: GitHub Fine-Grained Token (Recommended)

1. Create a token at: GitHub.com → Settings → Developer settings → Personal access tokens → Fine-grained tokens
2. Grant access to the repository with `Contents: Read` permission.
3. Clone using the token:

```bash
cd /opt/sms
sudo -u sms_admin git clone https://github.com/hudson254/SchoolManagementSystem.git /opt/sms
```

When prompted for a password, paste the token.

#### Option B: SSH Deploy Key

1. Generate an SSH key pair:

```bash
sudo -u sms_admin ssh-keygen -t ed25519 -C "sms-deploy-key" -f /home/sms_admin/.ssh/id_ed25519 -N ""
```

2. Display the public key:

```bash
sudo cat /home/sms_admin/.ssh/id_ed25519.pub
```

3. Add the key as a **deploy key** in the GitHub repository: Settings → Deploy keys → Add deploy key.
4. Clone using SSH:

```bash
sudo -u sms_admin git clone git@github.com:hudson254/SchoolManagementSystem.git /opt/sms
```

### 10.4 Verify the Clone

```bash
sudo -u sms_admin git -C /opt/sms status
ls -la /opt/sms/
```

### 10.5 Checkout the Correct Branch

```bash
sudo -u sms_admin git -C /opt/sms checkout main
```

---

## 11. Environment Configuration

### 11.1 Create the Production Environment File

```bash
sudo mkdir -p /opt/sms/env
sudo cp /opt/sms/.env.example /opt/sms/env/.env
sudo chown sms_admin:sms_admin /opt/sms/env/.env
sudo chmod 600 /opt/sms/env/.env
```

### 11.2 Edit the Environment File

```bash
sudo -u sms_admin nano /opt/sms/env/.env
```

### 11.3 Required Environment Variables

| Variable | Required | Description | Example |
|----------|----------|-------------|---------|
| `DB_PASSWORD` | **YES** | PostgreSQL database password | `<GENERATE-A-LONG-RANDOM-PASSWORD>` |
| `JWT_SECRET` | **YES** | JWT signing key (64+ characters) | `<GENERATE-A-64-CHAR-SECRET>` |
| `GRAFANA_PASSWORD` | **YES** | Grafana admin password | `<GENERATE-A-LONG-RANDOM-PASSWORD>` |
| `ADMIN_EMAIL` | **YES** | Initial system administrator email | `admin@school.edu` |
| `ADMIN_PASSWORD` | **YES** | Initial system administrator password | `<GENERATE-A-LONG-RANDOM-PASSWORD>` |

### 11.4 Optional Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `DB_NAME` | `SchoolManagementSystem` | Database name |
| `DB_USER` | `sms_user` | Database user |
| `JWT_ISSUER` | `SMSAPI` | JWT issuer |
| `JWT_AUDIENCE` | `SMSWeb` | JWT audience |
| `JWT_EXPIRY` | `60` | JWT expiry in minutes |
| `API_URL` | `http://localhost:5000/api` | API URL for frontend |
| `GRAFANA_URL` | `http://localhost:3001` | Grafana external URL |
| `BACKUP_INTERVAL` | `86400` | Backup interval in seconds (24 hours) |
| `BACKUP_RETENTION_DAYS` | `30` | Backup retention in days |
| `SMTP_HOST` | `smtp.gmail.com` | SMTP server (email disabled, can remain default) |
| `SMTP_PORT` | `587` | SMTP port |
| `SMTP_USERNAME` | `CHANGE_ME` | SMTP username |
| `SMTP_PASSWORD` | `CHANGE_ME` | SMTP password |
| `SMTP_FROM` | `CHANGE_ME` | SMTP from address |
| `ADMIN_FIRST_NAME` | `System` | Administrator first name |
| `ADMIN_LAST_NAME` | `Administrator` | Administrator last name |
| `Swagger__Enabled` | `false` | Enable Swagger in production |
| `ENABLE_MFA` | `false` | Enable multi-factor authentication |
| `ENABLE_PWA` | `true` | Enable Progressive Web App |
| `RATE_LIMIT_PERMIT` | `100` | Rate limit requests per window |
| `RATE_LIMIT_WINDOW` | `60` | Rate limit window in seconds |

### 11.5 Generating Strong Secrets on Debian 13

Generate a strong 64-character JWT secret:

```bash
openssl rand -base64 48 | tr -d '\n' | head -c 64
```

Generate a strong database password (32 characters):

```bash
openssl rand -base64 24 | tr -d '\n'
```

Generate a strong Grafana password:

```bash
openssl rand -base64 18 | tr -d '\n'
```

### 11.6 Variables That Must Be Changed

The following variables **must** be changed from their default values:

- `DB_PASSWORD`
- `JWT_SECRET`
- `GRAFANA_PASSWORD`
- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`

### 11.7 Variables That Can Remain at Defaults

- `JWT_ISSUER` — can remain `SMSAPI`
- `JWT_AUDIENCE` — can remain `SMSWeb`
- `JWT_EXPIRY` — can remain `60`
- `BACKUP_INTERVAL` — can remain `86400`
- `BACKUP_RETENTION_DAYS` — can remain `30`
- `Swagger__Enabled` — should remain `false` in production
- `ENABLE_MFA` — can remain `false`

### 11.8 Complete .env Example

```
# Database Configuration
DB_PASSWORD=Yx8pLm3KqR7vB2wN5sH9jF6cA1dE4gT
DB_NAME=SchoolManagementSystem
DB_USER=sms_user

# JWT Configuration
JWT_SECRET=a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2
JWT_ISSUER=SMSAPI
JWT_AUDIENCE=SMSWeb
JWT_EXPIRY=60

# SSL Configuration
SSL_PASSWORD=

# SMTP Configuration (email disabled, can remain unchanged)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=CHANGE_ME
SMTP_PASSWORD=CHANGE_ME
SMTP_FROM=CHANGE_ME

# Backup Configuration
BACKUP_INTERVAL=86400
BACKUP_RETENTION_DAYS=30

# Monitoring
GRAFANA_PASSWORD=K8mR2vX5pL9jH3wN

# Application
API_URL=http://localhost:5000/api
GRAFANA_URL=http://localhost:3001

# Administrator Credentials (REQUIRED for seeding)
ADMIN_EMAIL=admin@school.edu
ADMIN_PASSWORD=SuperStr0ng!Admin#Pass2024
ADMIN_FIRST_NAME=System
ADMIN_LAST_NAME=Administrator

# Swagger Configuration (disable in production)
Swagger__Enabled=false

# Features
ENABLE_MFA=false
ENABLE_PWA=true

# Rate Limiting
RATE_LIMIT_PERMIT=100
RATE_LIMIT_WINDOW=60
```

---

## 12. Database Preparation

### 12.1 Database Architecture

PostgreSQL runs as a Docker container defined in `docker/docker-compose.prod.yml`:

- **Image:** `postgres:16-alpine`
- **Container name:** `sms-postgres`
- **Internal port:** `5432`
- **External port:** `5433` (mapped to host)
- **Database name:** `SchoolManagementSystem`
- **Database user:** `sms_user`
- **Password:** From `DB_PASSWORD` environment variable
- **Data volume:** `postgres_data` (Docker managed volume)
- **Init script:** `./init-db.sql` mounted to `/docker-entrypoint-initdb.d/init.sql`

### 12.2 Database Initialization

The database is initialized automatically when the PostgreSQL container starts for the first time:

1. The container creates the database `SchoolManagementSystem`.
2. The container creates the user `sms_user` with the password from `DB_PASSWORD`.
3. The init script `docker/init-db.sql` runs automatically.

### 12.3 Database Migrations

Migrations are applied automatically when the API container starts, via `Program.cs`:

```csharp
await dbContext.Database.MigrateAsync();
```

To run migrations manually:

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll migrate-database
```

### 12.4 Database Seeding

Seeding populates the database with:
- Default tenant
- Roles (Administrator, COORDINATOR, Lecturer, Student, Receptionist)
- Initial system administrator account

The seed process is run automatically by the `DatabaseSeeder` class when the API starts for the first time.

To run seeding manually:

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll seed-data
```

### 12.5 Persistent Volumes

The following Docker volumes are created for persistent data:

| Volume Name | Purpose | Backup Required |
|-------------|---------|-----------------|
| `postgres_data` | PostgreSQL database files | **YES** |
| `api_logs` | API application logs | Optional |
| `api_data` | API application data | Optional |
| `api_uploads` | Uploaded files | **YES** |
| `backup_data` | Database backups | **YES** |
| `prometheus_data` | Prometheus time-series data | Optional |
| `grafana_data` | Grafana dashboards and settings | Optional |
| `alertmanager_data` | Alertmanager state | Optional |

### 12.6 Database Backup Configuration

Backups are handled by the `sms-backup` container, which runs `pg_dump` on a schedule:

```bash
# Backup interval: 24 hours (86400 seconds)
# Retention: 30 days
# Backup location: /backups (Docker volume)
```

### 12.7 Manual Database Backup

```bash
cd /opt/sms
sudo -u sms_admin docker exec sms-postgres pg_dump -U sms_user SchoolManagementSystem > /opt/sms/backups/manual_backup_$(date +%Y%m%d_%H%M%S).sql
```

### 12.8 Database Restore

```bash
cd /opt/sms
sudo -u sms_admin docker exec -i sms-postgres psql -U sms_user -d SchoolManagementSystem < /path/to/backup.sql
```

### 12.9 Database Health Check

```bash
docker exec sms-postgres pg_isready -U sms_user -d SchoolManagementSystem
```

**Expected output:** `/var/run/postgresql:5432 - accepting connections`

---

## 13. Initial System Administrator Account

### 13.1 How the Account is Created

The system administrator account is created automatically by the `DatabaseSeeder` class when the API starts for the first time. The seeder uses the following environment variables from `.env`:

- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`
- `ADMIN_FIRST_NAME`
- `ADMIN_LAST_NAME`

### 13.2 Prerequisites

Before the administrator account is created:

1. The PostgreSQL container must be running and healthy.
2. The API container must start successfully.
3. Database migrations must run.
4. The `DatabaseSeeder.SeedAsync()` method must execute.

All of these happen automatically when the application stack is started.

### 13.3 What the Seeder Creates

The `DatabaseSeeder` creates:

1. **Default tenant** — The multi-tenant context for the system.
2. **Roles** — Administrator, COORDINATOR, Lecturer, Student, Receptionist.
3. **System Administrator user** — With the email and password from environment variables.
4. **Administrator claims/permissions** — Full access to the system.

### 13.4 Initial Login

1. Open a browser and navigate to `https://school.example.lan` (or `http://school.example.lan` if HTTPS is not yet configured).
2. Click **Login**.
3. Enter the credentials:
   - **Email:** The value of `ADMIN_EMAIL` in `.env`
   - **Password:** The value of `ADMIN_PASSWORD` in `.env`
4. Click **Login**.

### 13.5 Changing the Temporary Password

**⚠️ IMPORTANT:** After the first login, the system administrator should change the password immediately.

1. Log in with the initial credentials.
2. Navigate to **Profile** or **Settings**.
3. Select **Change Password**.
4. Enter the current password.
5. Enter a new strong password (minimum 12 characters, must include uppercase, lowercase, digit, and special character).
6. Confirm the new password.
7. Click **Save**.

### 13.6 Verifying Administrator Permissions

After logging in, verify that you have full administrator access:

- The dashboard should show all administrative features.
- The sidebar should include **Administration**, **Users**, **Roles**, and **Settings** sections.
- You should be able to create courses, manage users, view reports, and access all system features.

### 13.7 Manual Seeding (if automatic seeding fails)

If the administrator account was not created automatically, run the seed command:

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll seed-data
```

---

## 14. Reverse Proxy and Application Access

### 14.1 Nginx Configuration

The Nginx reverse proxy is configured in `docker/nginx.conf`. The production deployment uses the version in `docker/docker-compose.prod.yml` which mounts the nginx configuration and TLS certificates.

### 14.2 Nginx Configuration File

The Nginx configuration is located at: `/opt/sms/docker/nginx.conf`

Key configuration details:

- **HTTP to HTTPS redirect** — All HTTP traffic on port 80 is redirected to HTTPS.
- **API proxy** — `/api/` requests are proxied to the API container at `api:80`.
- **SignalR hub** — `/hub/` requests are proxied for WebSocket support.
- **Swagger UI** — `/swagger/` requests are proxied (disabled by default).
- **Health checks** — `/health` requests are proxied without logging.
- **Frontend** — All other requests are proxied to the frontend container.
- **Static file caching** — Images, CSS, JS, and fonts are cached for 1 year.
- **Rate limiting** — API requests are limited to 10 requests per second, with a burst of 20.
- **Security headers** — X-Frame-Options, X-XSS-Protection, X-Content-Type-Options, Referrer-Policy.
- **Upload size** — Maximum client body size is 100 MB.

### 14.3 How the Application is Accessed

Users access the application using the LAN hostname, for example:

```
https://school.example.lan
```

The Nginx container handles the connection as follows:

1. Client connects to `https://school.example.lan` (port 443).
2. Nginx terminates TLS using the certificate at `/etc/ssl/certs/school.example.lan.crt`.
3. Nginx proxies the request to the appropriate upstream:
   - `/api/` → `api:80` (ASP.NET Core API)
   - `/hub/` → `api:80` (SignalR WebSocket)
   - `/health` → `api:80` (Health check)
   - `/` → `frontend:80` (React SPA)

### 14.4 Nginx Installation (for Host-Based Nginx)

Since the project uses a Docker-based Nginx container, there is no need to install Nginx on the host. The Nginx container is defined in the Docker Compose file.

However, if you need to edit the Nginx configuration:

```bash
sudo -u sms_admin nano /opt/sms/docker/nginx.conf
```

### 14.5 Testing Nginx Configuration

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env config
```

### 14.6 Restarting Nginx

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env restart nginx
```

---

## 15. TLS for LAN Deployment

### 15.1 TLS Architecture

Since the School Management System is deployed on an internal LAN (no public domain), TLS certificates must come from an **internal Certificate Authority (CA)** rather than a public CA like Let's Encrypt.

### 15.2 Recommended Approach: Internal Certificate Authority

**Recommended option:** Create an internal CA and issue a certificate for the LAN hostname.

#### Step 1: Create the Internal CA

```bash
cd /opt/sms/certs
sudo -u sms_admin openssl genrsa -aes256 -out ca-key.pem 4096
sudo -u sms_admin openssl req -x509 -new -nodes -key ca-key.pem -sha256 -days 3650 -out ca-cert.pem \
    -subj "/C=XX/ST=State/L=City/O=Organization/CN=School Management System Internal CA"
```

**Note:** You will be prompted for a passphrase for the CA key. Store this securely.

#### Step 2: Create the Server Certificate

```bash
cd /opt/sms/certs
sudo -u sms_admin openssl genrsa -out school.example.lan.key 2048
sudo -u sms_admin openssl req -new -key school.example.lan.key -out school.example.lan.csr \
    -subj "/C=XX/ST=State/L=City/O=Organization/CN=school.example.lan"
```

#### Step 3: Create a Configuration File for SAN (Subject Alternative Name)

```bash
sudo -u sms_admin nano /opt/sms/certs/school.example.lan.ext
```

```
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment, dataEncipherment
subjectAltName = @alt_names

[alt_names]
DNS.1 = school.example.lan
DNS.2 = sms-server.example.lan
DNS.3 = localhost
IP.1 = 192.168.1.100
```

#### Step 4: Sign the Certificate

```bash
cd /opt/sms/certs
sudo -u sms_admin openssl x509 -req -in school.example.lan.csr -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out school.example.lan.crt -days 365 -sha256 -extfile school.example.lan.ext
```

#### Step 5: Set Correct Permissions

```bash
sudo chmod 600 /opt/sms/certs/school.example.lan.key
sudo chmod 644 /opt/sms/certs/school.example.lan.crt
sudo chown -R sms_admin:sms_admin /opt/sms/certs/
```

### 15.3 Alternative: Self-Signed Certificate

If an internal CA is not feasible, a self-signed certificate can be used:

```bash
cd /opt/sms/certs
sudo -u sms_admin openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout school.example.lan.key -out school.example.lan.crt \
    -subj "/CN=school.example.lan" \
    -addext "subjectAltName=DNS:school.example.lan,DNS:sms-server.example.lan,IP:192.168.1.100"
```

**⚠️ Note:** Self-signed certificates will cause browser security warnings. Users must manually trust the certificate (see Section 15.5).

### 15.4 Updating Nginx Configuration

The Nginx configuration in `docker/docker-compose.prod.yml` mounts certificates from `/etc/ssl/certs:/etc/ssl/certs:ro` and `/etc/ssl/private:/etc/ssl/private:ro`.

Copy the certificates to the standard locations:

```bash
sudo cp /opt/sms/certs/school.example.lan.crt /etc/ssl/certs/
sudo cp /opt/sms/certs/school.example.lan.key /etc/ssl/private/
```

Update the Nginx configuration to use the correct certificate paths:

```bash
sudo -u sms_admin nano /opt/sms/docker/nginx.conf
```

Find and update the SSL configuration:

```
ssl_certificate /etc/ssl/certs/school.example.lan.crt;
ssl_certificate_key /etc/ssl/private/school.example.lan.key;
```

### 15.5 Client Trust Requirements

For browsers to trust the internal CA certificate, the CA certificate must be installed on each client device.

#### Windows

1. Double-click the `ca-cert.pem` file.
2. Select **Install Certificate**.
3. Choose **Local Machine**.
4. Select **Place all certificates in the following store**.
5. Click **Browse** and select **Trusted Root Certification Authorities**.
6. Click **OK** → **Next** → **Finish**.

#### Android

1. Copy `ca-cert.pem` to the device.
2. Go to **Settings** → **Security** → **Encryption & credentials** → **Install a certificate**.
3. Select **CA certificate**.
4. Browse to the `ca-cert.pem` file.
5. Confirm installation.

#### iPhone/iPad

1. Send `ca-cert.pem` to the device (email, AirDrop, or web download).
2. Go to **Settings** → **General** → **Profiles** → **Install profile**.
3. After installation, go to **Settings** → **General** → **About** → **Certificate Trust Settings**.
4. Enable the installed CA certificate.

#### Linux

```bash
sudo cp /opt/sms/certs/ca-cert.pem /usr/local/share/ca-certificates/sms-ca.crt
sudo update-ca-certificates
```

### 15.6 Certificate Renewal

The certificate expires in 365 days. Set a calendar reminder to renew it before expiration.

Renewal process:

```bash
cd /opt/sms/certs
# Generate new CSR and sign (using the same CA)
sudo -u sms_admin openssl req -new -key school.example.lan.key -out school.example.lan.csr \
    -subj "/C=XX/ST=State/L=City/O=Organization/CN=school.example.lan"
sudo -u sms_admin openssl x509 -req -in school.example.lan.csr -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out school.example.lan.crt -days 365 -sha256 -extfile school.example.lan.ext

# Copy to SSL directory
sudo cp /opt/sms/certs/school.example.lan.crt /etc/ssl/certs/

# Restart Nginx
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env restart nginx
```

---

## 16. Monitoring and Alerting

### 16.1 Monitoring Stack Overview

The production deployment includes a full monitoring stack:

| Component | Image | Purpose |
|-----------|-------|---------|
| Prometheus | `prom/prometheus:v2.54.1` | Metrics collection and storage |
| Grafana | `grafana/grafana:11.2.0` | Visualization and dashboards |
| Alertmanager | `prom/alertmanager:v0.27.0` | Alert routing and notifications |
| Node Exporter | `prom/node-exporter:v1.8.2` | Host-level metrics (CPU, memory, disk) |
| Postgres Exporter | `prometheuscommunity/postgres-exporter:v0.15.0` | PostgreSQL metrics |
| cAdvisor | `gcr.io/cadvisor/cadvisor:v0.49.1` | Container-level metrics |

### 16.2 Persistent Storage

Monitoring data is stored in Docker volumes:

| Volume | Purpose |
|--------|---------|
| `prometheus_data` | Time-series metrics data |
| `grafana_data` | Grafana configuration and dashboards |
| `alertmanager_data` | Alert state |

### 16.3 Grafana Authentication

Grafana is configured with:

- **Admin user:** `admin` (configurable via `GRAFANA_USER` environment variable)
- **Admin password:** From `GRAFANA_PASSWORD` environment variable

### 16.4 Accessing Grafana

Grafana is accessible at: `http://school.example.lan:3001`

**⚠️ IMPORTANT:** The firewall should restrict access to Grafana to administrators only (see Section 8.5).

### 16.5 Pre-Provisioned Dashboards

Grafana dashboards are pre-provisioned from the `docker/grafana-dashboards/` directory. The dashboard provider configuration is at `docker/grafana-dashboards/dashboard-provider.yml`.

### 16.6 Pre-Provisioned Datasources

The Prometheus datasource is pre-configured in `docker/grafana-datasources/datasource.yml`.

### 16.7 Alertmanager Configuration

Alertmanager is configured in `docker/alertmanager.yml`. By default, alerts are routed to a default receiver. Configure the receiver (e.g., email, Slack, webhook) in this file.

### 16.8 Prometheus Alert Rules

Alert rules are defined in `docker/prometheus-alerts.yml`. These include alerts for:

- API service down
- High API response latency
- PostgreSQL connectivity issues
- Node exporter unavailable
- High disk usage
- High memory usage
- Container restarts

### 16.9 Health Checks

Each container has a health check defined in the Docker Compose file:

- **PostgreSQL:** `pg_isready -U sms_user -d SchoolManagementSystem`
- **API:** `curl --silent --fail http://localhost/health || exit 1`
- **Nginx:** Nginx does not have a built-in health check; the API health check is used instead.

### 16.10 Access Restrictions

The following monitoring components should NOT be exposed to ordinary LAN users:

- Prometheus (port 9090) — administrators only
- Alertmanager (port 9093) — administrators only
- Node Exporter (port 9100) — Docker internal only
- Postgres Exporter (port 9187) — Docker internal only
- cAdvisor (port 8080) — Docker internal only

Only Grafana (port 3001) should be accessible to authorized administrators on the LAN.

---

## 17. Backup and Disaster Recovery Preparation

### 17.1 What Needs to be Backed Up

| Data | Location | Backup Method | Priority |
|------|----------|--------------|----------|
| PostgreSQL database | `postgres_data` volume | `pg_dump` via backup container | **Critical** |
| Uploaded files | `api_uploads` volume | File copy / rsync | **Critical** |
| Environment configuration | `/opt/sms/env/.env` | File copy | **Critical** |
| TLS certificates | `/opt/sms/certs/` | File copy | **Critical** |
| Docker Compose files | `/opt/sms/docker/` | Git repository (already backed up) | Important |
| Monitoring configuration | Grafana dashboards, Prometheus rules | Git repository | Important |
| Prometheus data | `prometheus_data` volume | Optional (can be regenerated) | Low |
| Grafana data | `grafana_data` volume | Optional (can be regenerated) | Low |

### 17.2 Automated Backups

The `sms-backup` container automatically backs up the PostgreSQL database:

```bash
# Backup interval: 24 hours (configurable via BACKUP_INTERVAL)
# Retention: 30 days (configurable via BACKUP_RETENTION_DAYS)
# Backup location: /backups (Docker volume)
```

### 17.3 Manual Database Backup

```bash
cd /opt/sms
sudo -u sms_admin docker exec sms-postgres pg_dump -U sms_user -d SchoolManagementSystem -F c -f /tmp/db_backup_$(date +%Y%m%d).dump
sudo -u sms_admin docker cp sms-postgres:/tmp/db_backup_$(date +%Y%m%d).dump /opt/sms/backups/
```

### 17.4 Backup Script

The project provides a backup script at `scripts/backup.sh`. To use it:

```bash
cd /opt/sms
sudo -u sms_admin bash scripts/backup.sh
```

### 17.5 Backup Directory Structure

```
/opt/sms/backups/
├── database/          # PostgreSQL dumps (from backup container)
│   ├── sms-db-2024-01-01.sql.gz
│   ├── sms-db-2024-01-02.sql.gz
│   └── ...
├── uploads/           # Uploaded files (manual copies)
├── config/            # Environment and configuration files
│   └── env-backup-2024-01-01.tar.gz
└── certificates/      # TLS certificate backups
    └── certs-backup-2024-01-01.tar.gz
```

### 17.6 Backup Frequency and Retention

| Backup Type | Frequency | Retention | Location |
|-------------|-----------|-----------|----------|
| Database (automated) | Every 24 hours | 30 days | Docker volume + external copy |
| Database (manual) | Before major changes | Until next manual backup | `/opt/sms/backups/` |
| Configuration | After any change | 6 months | `/opt/sms/backups/` |
| Certificates | After renewal | 3 years | `/opt/sms/backups/` |
| Uploaded files | Daily | 30 days | External storage |

### 17.7 Important: Backups Must Not Exist Only on the Same Physical Disk

**⚠️ CRITICAL:** Backups stored only on the same server's disk will be lost if the disk fails.

**Recommended backup destinations:**
- Network-attached storage (NAS)
- Another server on the LAN
- External USB drive (rotated regularly)
- Cloud storage (if internet is available)

### 17.8 Testing Restoration

Periodically test the backup restoration process:

```bash
# Restore database backup
cd /opt/sms
sudo -u sms_admin docker exec -i sms-postgres pg_restore -U sms_user -d SchoolManagementSystem -c < /opt/sms/backups/database/sms-db-2024-01-01.sql
```

---

## 18. System Security Hardening

### 18.1 SSH Configuration

Edit `/etc/ssh/sshd_config`:

```bash
sudo nano /etc/ssh/sshd_config
```

Set the following:

```
# Disable root login
PermitRootLogin no

# Use key-based authentication only
PubkeyAuthentication yes
PasswordAuthentication no

# Disable empty passwords
PermitEmptyPasswords no

# Use SSH protocol 2
Protocol 2

# Limit login attempts
MaxAuthTries 3

# Disable X11 forwarding
X11Forwarding no

# Allow only specific users
AllowUsers admin sms_admin
```

Restart SSH:

```bash
sudo systemctl restart sshd
```

### 18.2 SSH Key Authentication

Generate an SSH key pair on your local machine (not the server):

```bash
ssh-keygen -t ed25519 -C "your-email@example.com"
```

Copy the public key to the server:

```bash
ssh-copy-id -i ~/.ssh/id_ed25519.pub admin@192.168.1.100
```

### 18.3 Disable Password Login

After verifying key-based authentication works, disable password login:

```bash
sudo nano /etc/ssh/sshd_config
```

Set:

```
PasswordAuthentication no
```

Restart SSH:

```bash
sudo systemctl restart sshd
```

### 18.4 Root SSH Login Restrictions

Root login is already disabled by `PermitRootLogin no`. For administrative tasks, use `sudo`:

```bash
sudo command
```

### 18.5 Fail2ban

Fail2ban is already installed (from Section 4.4). Configure it for SSH:

```bash
sudo nano /etc/fail2ban/jail.local
```

```
[sshd]
enabled = true
port = ssh
filter = sshd
logpath = /var/log/auth.log
maxretry = 3
bantime = 3600
findtime = 600
```

Restart Fail2ban:

```bash
sudo systemctl restart fail2ban
```

### 18.6 Firewall

The firewall is configured in Section 8. Ensure it is enabled:

```bash
sudo ufw status verbose
```

### 18.7 Automatic Security Updates

Install and configure `unattended-upgrades`:

```bash
sudo apt install -y unattended-upgrades apt-listchanges
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

Select **Yes** when prompted.

### 18.8 File Permissions

Ensure correct permissions on sensitive files:

```bash
sudo chmod 600 /opt/sms/env/.env
sudo chmod 600 /opt/sms/certs/*.key
sudo chmod 644 /opt/sms/certs/*.crt
sudo chmod 750 /opt/sms/scripts/*.sh
```

### 18.9 Docker Security Considerations

- The `sms_admin` user is in the Docker group. This is necessary for deployment but grants effective root access.
- Only use trusted Docker images (official images from Docker Hub).
- Regularly update Docker images:

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml pull
```

### 18.10 Secrets Protection

- The `.env` file contains all secrets. It is readable only by `sms_admin` (permissions 600).
- Never commit `.env` to Git (it is in `.gitignore`).
- JWT secret is 64+ characters of random data.
- Database password is a strong random string.
- Grafana password is a strong random string.

### 18.11 Log Management

Log rotation is configured for Docker containers (100 MB per file, 5 files retained). System logs are managed by `logrotate`:

```bash
sudo nano /etc/logrotate.conf
```

### 18.12 Time Synchronization

Time synchronization is configured in Section 4.6. Verify:

```bash
timedatectl
chronyc tracking
```

### 18.13 Unnecessary Services

Check for and disable unnecessary services:

```bash
sudo systemctl list-units --type=service --state=running
```

Common services that can be disabled on a dedicated server:

```bash
sudo systemctl disable --now bluetooth.service 2>/dev/null || true
sudo systemctl disable --now cups.service 2>/dev/null || true
sudo systemctl disable --now avahi-daemon.service 2>/dev/null || true
```

### 18.14 System Updates

Regularly update the system:

```bash
sudo apt update
sudo apt upgrade -y
sudo apt autoremove -y
```

### 18.15 Reboot After Kernel Updates

After a kernel update, reboot the server:

```bash
sudo reboot
```

---

## 19. System Resource and Kernel Configuration

### 19.1 File Descriptor Limits

The School Management System does not require custom file descriptor limits for normal operation. The default Debian 13 limits are sufficient.

To verify current limits:

```bash
ulimit -n
```

### 19.2 Memory Settings

The Docker Compose file defines memory limits for each container:

| Container | Memory Limit | Memory Reservation |
|-----------|-------------|-------------------|
| PostgreSQL | 4 GB | 2 GB |
| API | 2 GB | 1 GB |
| Frontend | 512 MB | 256 MB |
| Nginx | 256 MB | 128 MB |
| Backup | 256 MB | 128 MB |
| Prometheus | (unlimited) | (unlimited) |
| Grafana | (unlimited) | (unlimited) |
| Alertmanager | 256 MB | 128 MB |
| Node Exporter | 256 MB | 128 MB |
| Postgres Exporter | 256 MB | 128 MB |
| cAdvisor | 256 MB | 128 MB |

### 19.3 PostgreSQL Kernel Parameters

PostgreSQL inside Docker uses the default kernel parameters. For production workloads, consider adjusting:

```bash
sudo nano /etc/sysctl.d/30-postgresql.conf
```

```
# PostgreSQL recommended kernel parameters
kernel.shmmax = 4294967296
kernel.shmall = 1048576
vm.overcommit_memory = 0
```

Apply:

```bash
sudo sysctl -p /etc/sysctl.d/30-postgresql.conf
```

### 19.4 Docker Storage Settings

Docker uses the `overlay2` storage driver (default). The Docker root directory is at `/var/lib/docker/`. Monitor disk usage:

```bash
sudo du -sh /var/lib/docker/
```

### 19.5 Log Rotation

Docker container logs are configured to rotate at 100 MB per file, with 5 files retained. This is configured in `docker-compose.prod.yml` for each service.

System logs are rotated by `logrotate`. Verify:

```bash
cat /etc/logrotate.conf
```

### 19.6 Disk Space Monitoring

Monitor disk space usage:

```bash
df -h
du -sh /opt/sms/
du -sh /var/lib/docker/
```

### 19.7 Swap Configuration

Swap is configured during Debian installation. Verify:

```bash
swapon --show
free -h
```

### 19.8 System Limits

The default Debian 13 system limits are sufficient for the School Management System. No additional kernel tuning is required.

---

## 20. Verification Checklist

Run each verification command and verify the expected output.

### 20.1 Debian Version

```bash
cat /etc/debian_version
```

**Expected:** `13.0` or similar

### 20.2 CPU

```bash
nproc
lscpu | grep "Model name"
```

**Expected:** At least 4 cores

### 20.3 RAM

```bash
free -h
```

**Expected:** At least 8 GB total

### 20.4 Disk

```bash
df -h
```

**Expected:** At least 100 GB available

### 20.5 Network

```bash
ip addr show
```

**Expected:** Interface with static IP address configured

### 20.6 Static IP

```bash
ip addr show | grep "inet "
```

**Expected:** Static IP address (not DHCP)

### 20.7 DNS

```bash
nslookup school.example.lan
```

**Expected:** Resolves to the server's IP address

### 20.8 Hostname

```bash
hostnamectl
```

**Expected:** `Static hostname: sms-server`

### 20.9 Internet Access

```bash
ping -c 4 8.8.8.8
```

**Expected:** Successful ping responses

### 20.10 Docker

```bash
docker --version
```

**Expected:** `Docker version 27.x.x`

### 20.11 Docker Compose

```bash
docker compose version
```

**Expected:** `Docker Compose version v2.x.x`

### 20.12 Docker Service

```bash
sudo systemctl is-active docker
```

**Expected:** `active`

### 20.13 Firewall

```bash
sudo ufw status verbose
```

**Expected:** Status: active, with appropriate rules

### 20.14 Git

```bash
git --version
```

**Expected:** `git version 2.x.x`

### 20.15 Application Directory

```bash
ls -la /opt/sms/
```

**Expected:** Directory structure with proper ownership

### 20.16 File Permissions

```bash
ls -la /opt/sms/env/.env
```

**Expected:** `-rw------- 1 sms_admin sms_admin`

### 20.17 Environment File

```bash
sudo -u sms_admin cat /opt/sms/env/.env | grep -v "^#" | grep -v "^$"
```

**Expected:** All required variables are set

### 20.18 Required Secrets

```bash
sudo -u sms_admin grep -E "^(DB_PASSWORD|JWT_SECRET|GRAFANA_PASSWORD|ADMIN_PASSWORD)=" /opt/sms/env/.env
```

**Expected:** No placeholder values (no "CHANGE_ME")

### 20.19 PostgreSQL

```bash
docker ps --filter "name=sms-postgres" --format "{{.Names}}"
```

**Expected:** `sms-postgres`

### 20.20 Redis

```bash
docker ps --filter "name=redis" --format "{{.Names}}"
```

**Expected:** Empty (Redis is optional and not deployed by default)

### 20.21 Nginx

```bash
docker ps --filter "name=sms-nginx" --format "{{.Names}}"
```

**Expected:** `sms-nginx`

### 20.22 TLS

```bash
ls -la /etc/ssl/certs/school.example.lan.crt
ls -la /etc/ssl/private/school.example.lan.key
```

**Expected:** Both files exist

### 20.23 LAN DNS

```bash
nslookup school.example.lan 192.168.1.1
```

**Expected:** Resolves to the server IP

### 20.24 Prometheus

```bash
docker ps --filter "name=sms-prometheus" --format "{{.Names}}"
```

**Expected:** `sms-prometheus`

### 20.25 Grafana

```bash
docker ps --filter "name=sms-grafana" --format "{{.Names}}"
```

**Expected:** `sms-grafana`

### 20.26 Alertmanager

```bash
docker ps --filter "name=sms-alertmanager" --format "{{.Names}}"
```

**Expected:** `sms-alertmanager`

### 20.27 Exporters

```bash
docker ps --filter "name=sms-node-exporter" --format "{{.Names}}"
docker ps --filter "name=sms-postgres-exporter" --format "{{.Names}}"
docker ps --filter "name=sms-cadvisor" --format "{{.Names}}"
```

**Expected:** All three containers are running

### 20.28 Backups

```bash
ls -la /opt/sms/backups/
```

**Expected:** Directory exists and is writable

### 20.29 System Administrator Seeding

```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env exec api dotnet SMS.API.dll seed-data 2>&1 | tail -5
```

**Expected:** `Database seeding completed successfully!`

### 20.30 Application Health Endpoint

```bash
curl -sf http://localhost:5000/health
```

**Expected:** JSON response with `"status": "Healthy"`

### 20.31 Frontend

```bash
curl -sf http://localhost:3000 | head -5
```

**Expected:** HTML content (React app)

### 20.32 API

```bash
curl -sf http://localhost:5000/api/v1/health | python3 -m json.tool
```

**Expected:** JSON response

### 20.33 Database Connectivity

```bash
docker exec sms-postgres pg_isready -U sms_user -d SchoolManagementSystem
```

**Expected:** `/var/run/postgresql:5432 - accepting connections`

### 20.34 Authentication

```bash
curl -sf -X POST http://localhost:5000/api/v1/auth/login \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"admin@school.edu\",\"password\":\"<ADMIN_PASSWORD>\"}"
```

**Expected:** JSON response with JWT token

### 20.35 Monitoring

```bash
curl -sf http://localhost:9090/api/v1/query?query=up
```

**Expected:** Prometheus query response

### 20.36 Reboot Persistence

```bash
sudo reboot
# Wait for server to come back up
ssh admin@192.168.1.100
docker ps
```

**Expected:** All containers are running after reboot

---

## 21. Deployment Readiness Test

### SERVER READY FOR SCHOOL MANAGEMENT SYSTEM DEPLOYMENT

Run this final test procedure to verify the server is ready for the actual application deployment.

### 21.1 Pre-Flight Check

```bash
#!/bin/bash
# Run this script as root or with sudo

echo "=== SMS Deployment Readiness Test ==="
echo ""

# 1. Debian version
echo "[1/15] Debian version..."
cat /etc/debian_version | grep -q "13" && echo "  PASS" || echo "  FAIL"

# 2. Docker
echo "[2/15] Docker..."
docker --version > /dev/null 2>&1 && echo "  PASS" || echo "  FAIL"

# 3. Docker Compose
echo "[3/15] Docker Compose..."
docker compose version > /dev/null 2>&1 && echo "  PASS" || echo "  FAIL"

# 4. Docker running
echo "[4/15] Docker daemon..."
systemctl is-active docker | grep -q "active" && echo "  PASS" || echo "  FAIL"

# 5. Git
echo "[5/15] Git..."
git --version > /dev/null 2>&1 && echo "  PASS" || echo "  FAIL"

# 6. Application directory
echo "[6/15] Application directory..."
test -d /opt/sms && echo "  PASS" || echo "  FAIL"

# 7. Environment file
echo "[7/15] Environment file..."
test -f /opt/sms/env/.env && echo "  PASS" || echo "  FAIL"

# 8. No placeholder secrets
echo "[8/15] No placeholder secrets..."
! grep -q "CHANGE_ME" /opt/sms/env/.env 2>/dev/null && echo "  PASS" || echo "  FAIL"

# 9. Firewall
echo "[9/15] Firewall..."
ufw status | grep -q "active" && echo "  PASS" || echo "  FAIL"

# 10. Ports
echo "[10/15] Ports 80/443 open..."
ufw status | grep -q "80/tcp" && ufw status | grep -q "443/tcp" && echo "  PASS" || echo "  FAIL"

# 11. TLS certificates
echo "[11/15] TLS certificates..."
test -f /etc/ssl/certs/school.example.lan.crt && test -f /etc/ssl/private/school.example.lan.key && echo "  PASS" || echo "  FAIL"

# 12. DNS resolution
echo "[12/15] DNS resolution..."
nslookup school.example.lan > /dev/null 2>&1 && echo "  PASS" || echo "  FAIL"

# 13. Backups directory
echo "[13/15] Backups directory..."
test -d /opt/sms/backups && echo "  PASS" || echo "  FAIL"

# 14. sms_admin user
echo "[14/15] sms_admin user..."
id sms_admin > /dev/null 2>&1 && echo "  PASS" || echo "  FAIL"

# 15. Docker group membership
echo "[15/15] Docker group membership..."
groups sms_admin | grep -q docker && echo "  PASS" || echo "  FAIL"

echo ""
echo "=== Test Complete ==="
```

Save this script:

```bash
sudo -u sms_admin nano /opt/sms/scripts/readiness-test.sh
```

Make it executable and run:

```bash
sudo chmod +x /opt/sms/scripts/readiness-test.sh
sudo bash /opt/sms/scripts/readiness-test.sh
```

**All 15 checks must pass** before proceeding with the deployment.

---

## 22. Troubleshooting

### 22.1 Docker Service Fails to Start

**Symptoms:** `systemctl status docker` shows `failed` or `inactive`.

**Diagnostic command:**
```bash
sudo journalctl -u docker -n 50 --no-pager
```

**Likely cause:** Invalid configuration in `/etc/docker/daemon.json`.

**Corrective action:**
```bash
sudo mv /etc/docker/daemon.json /etc/docker/daemon.json.bak
sudo systemctl restart docker
```

**Verification:**
```bash
sudo systemctl status docker
```

### 22.2 Invalid /etc/docker/daemon.json

**Symptoms:** Docker fails to start; JSON syntax error in logs.

**Diagnostic command:**
```bash
python3 -m json.tool /etc/docker/daemon.json
```

**Likely cause:** Missing comma, extra comma, or unquoted key.

**Corrective action:**
```bash
sudo nano /etc/docker/daemon.json
# Fix the JSON syntax
python3 -m json.tool /etc/docker/daemon.json
sudo systemctl restart docker
```

**Verification:**
```bash
docker info
```

### 22.3 Docker Compose Cannot Find the Compose File

**Symptoms:** `docker compose -f docker/docker-compose.prod.yml up -d` returns `file not found`.

**Diagnostic command:**
```bash
ls -la /opt/sms/docker/docker-compose.prod.yml
```

**Likely cause:** Wrong working directory.

**Corrective action:**
```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env up -d
```

**Verification:**
```bash
docker ps
```

### 22.4 Incorrect Working Directory

**Symptoms:** Scripts fail with `file not found` errors.

**Diagnostic command:**
```bash
pwd
```

**Likely cause:** Running commands from the wrong directory.

**Corrective action:**
```bash
cd /opt/sms
```

**Verification:**
```bash
ls -la docker/docker-compose.prod.yml
```

### 22.5 Environment Variables Are Missing

**Symptoms:** Docker Compose fails with `variable is not set` errors.

**Diagnostic command:**
```bash
sudo -u sms_admin grep -E "^(DB_PASSWORD|JWT_SECRET|GRAFANA_PASSWORD|ADMIN_EMAIL|ADMIN_PASSWORD)=" /opt/sms/env/.env
```

**Likely cause:** Missing variables in `.env` file.

**Corrective action:**
```bash
sudo -u sms_admin nano /opt/sms/env/.env
# Add the missing variables
```

**Verification:**
```bash
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env config > /dev/null && echo "Valid"
```

### 22.6 PostgreSQL Port Mismatch

**Symptoms:** API cannot connect to PostgreSQL; `Connection refused` errors.

**Diagnostic command:**
```bash
docker ps --filter "name=sms-postgres" --format "{{.Ports}}"
```

**Likely cause:** PostgreSQL is running on a different port than expected.

**Expected output:** `0.0.0.0:5433->5432/tcp`

**Corrective action:**
```bash
# Check if PostgreSQL is running on the correct port
docker exec sms-postgres ss -tlnp | grep 5432
```

**Verification:**
```bash
docker exec sms-postgres pg_isready -U sms_user -d SchoolManagementSystem
```

### 22.7 Permission Denied Errors

**Symptoms:** Cannot write to `/opt/sms/` or its subdirectories.

**Diagnostic command:**
```bash
ls -la /opt/sms/
whoami
```

**Likely cause:** Running commands as the wrong user.

**Corrective action:**
```bash
sudo chown -R sms_admin:sms_admin /opt/sms/
sudo -u sms_admin command
```

**Verification:**
```bash
sudo -u sms_admin touch /opt/sms/test.txt && rm /opt/sms/test.txt
```

### 22.8 sms_admin Already Exists

**Symptoms:** `useradd: user sms_admin already exists`.

**Diagnostic command:**
```bash
id sms_admin
```

**Likely cause:** The user was created in a previous attempt.

**Corrective action:**
```bash
# Verify the existing user is correct
id sms_admin
groups sms_admin
# If needed, add to docker group
sudo usermod -aG docker sms_admin
```

**Verification:**
```bash
groups sms_admin | grep docker
```

### 22.9 Docker Group Does Not Exist

**Symptoms:** `groupadd: group 'docker' already exists` or `usermod: group 'docker' does not exist`.

**Diagnostic command:**
```bash
getent group docker
```

**Likely cause:** Docker is not installed yet.

**Corrective action:**
```bash
# Install Docker first (Section 6)
sudo apt install -y docker-ce
```

**Verification:**
```bash
getent group docker
```

### 22.10 DNS Hostname Does Not Resolve

**Symptoms:** `nslookup school.example.lan` returns `server failed` or `Non-existent domain`.

**Diagnostic command:**
```bash
nslookup school.example.lan 192.168.1.1
```

**Likely cause:** Omada DNS entry not configured or not propagated.

**Corrective action:**
1. Verify the DNS entry in Omada Controller.
2. Wait 1-2 minutes for propagation.
3. Flush client DNS cache:
   - Windows: `ipconfig /flushdns`
   - Linux: `sudo systemd-resolve --flush-caches`

**Verification:**
```bash
nslookup school.example.lan
```

### 22.11 Nginx Configuration Errors

**Symptoms:** Nginx container fails to start or returns 502 Bad Gateway.

**Diagnostic command:**
```bash
cd /opt/sms
sudo -u sms_admin docker logs sms-nginx
```

**Likely cause:** Syntax error in `nginx.conf` or missing upstream server.

**Corrective action:**
```bash
sudo -u sms_admin nano /opt/sms/docker/nginx.conf
# Fix the configuration
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env restart nginx
```

**Verification:**
```bash
curl -sf http://localhost/health
```

### 22.12 TLS Certificate Errors

**Symptoms:** Browser shows security warning or `NET::ERR_CERT_COMMON_NAME_INVALID`.

**Diagnostic command:**
```bash
openssl x509 -in /etc/ssl/certs/school.example.lan.crt -text -noout | grep -A1 "Subject:"
```

**Likely cause:** Certificate CN does not match the hostname.

**Corrective action:**
```bash
# Regenerate certificate with correct hostname
cd /opt/sms/certs
# ... (follow Section 15.2)
```

**Verification:**
```bash
openssl verify -CAfile /opt/sms/certs/ca-cert.pem /opt/sms/certs/school.example.lan.crt
```

### 22.13 Container Health Checks Fail

**Symptoms:** Container status shows `unhealthy`.

**Diagnostic command:**
```bash
docker inspect --format='{{.State.Health.Status}}' sms-api
```

**Likely cause:** Application failed to start; database not ready.

**Corrective action:**
```bash
docker logs sms-api
docker logs sms-postgres
```

**Verification:**
```bash
docker inspect --format='{{.State.Health.Status}}' sms-api
```

### 22.14 Database Migration Failure

**Symptoms:** API logs show `Error applying migrations`.

**Diagnostic command:**
```bash
cd /opt/sms
sudo -u sms_admin docker logs sms-api 2>&1 | grep -i migration
```

**Likely cause:** PostgreSQL is not ready; connection string is wrong.

**Corrective action:**
```bash
# Verify PostgreSQL is healthy
docker exec sms-postgres pg_isready -U sms_user -d SchoolManagementSystem

# Run migration manually
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll migrate-database
```

**Verification:**
```bash
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -c "\dt" | wc -l
```

### 22.15 Database Seed Failure

**Symptoms:** API logs show `Error seeding database`.

**Diagnostic command:**
```bash
cd /opt/sms
sudo -u sms_admin docker logs sms-api 2>&1 | grep -i seed
```

**Likely cause:** Missing `ADMIN_EMAIL` or `ADMIN_PASSWORD` in environment.

**Corrective action:**
```bash
# Verify environment variables
sudo -u sms_admin grep -E "^(ADMIN_EMAIL|ADMIN_PASSWORD)=" /opt/sms/env/.env

# Run seed manually
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll seed-data
```

**Verification:**
```bash
docker exec sms-postgres psql -U sms_user -d SchoolManagementSystem -c "SELECT COUNT(*) FROM \"Users\";"
```

### 22.16 Application Cannot Connect to PostgreSQL

**Symptoms:** API logs show `Connection refused` or `could not connect to server`.

**Diagnostic command:**
```bash
cd /opt/sms
sudo -u sms_admin docker logs sms-api 2>&1 | grep -i "connection"
```

**Likely cause:** Connection string is incorrect; PostgreSQL container is not running.

**Corrective action:**
```bash
# Verify PostgreSQL is running
docker ps --filter "name=sms-postgres"

# Check connection string in environment
sudo -u sms_admin grep DB_PASSWORD /opt/sms/env/.env

# Verify network connectivity
docker exec sms-api ping -c 2 postgres
```

**Verification:**
```bash
curl -sf http://localhost:5000/health
```

### 22.17 Redis Connection Failure

**Symptoms:** API logs show `Redis connection failed`.

**Diagnostic command:**
```bash
cd /opt/sms
sudo -u sms_admin grep "RedisTokenRevocation" /opt/sms/env/.env
```

**Likely cause:** Redis is configured but not running.

**Corrective action:**
```bash
# If Redis is not required, remove the RedisTokenRevocation:ConnectionString
# from the environment or the appsettings.Production.json
sudo -u sms_admin nano /opt/sms/env/.env
```

**Verification:**
```bash
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env restart api
```

### 22.18 Grafana Cannot Connect to Prometheus

**Symptoms:** Grafana dashboards show `Datasource error`.

**Diagnostic command:**
```bash
curl -sf http://localhost:9090/api/v1/query?query=up
```

**Likely cause:** Prometheus is not running or datasource configuration is wrong.

**Corrective action:**
```bash
# Verify Prometheus is running
docker ps --filter "name=sms-prometheus"

# Check datasource configuration
cat /opt/sms/docker/grafana-datasources/datasource.yml
```

**Verification:**
```bash
# Access Grafana and check datasource health
curl -u admin:<GRAFANA_PASSWORD> http://localhost:3001/api/datasources
```

### 22.19 Prometheus Exporters Unavailable

**Symptoms:** Prometheus targets show `DOWN` in the Targets page.

**Diagnostic command:**
```bash
curl -sf http://localhost:9100/metrics | head -5
curl -sf http://localhost:9187/metrics | head -5
curl -sf http://localhost:8080/metrics | head -5
```

**Likely cause:** Exporter containers are not running.

**Corrective action:**
```bash
docker ps --filter "name=sms-node-exporter"
docker ps --filter "name=sms-postgres-exporter"
docker ps --filter "name=sms-cadvisor"
```

**Verification:**
```bash
curl -sf http://localhost:9090/api/v1/targets
```

### 22.20 Firewall Blocks Required Traffic

**Symptoms:** Cannot access the application from LAN clients.

**Diagnostic command:**
```bash
sudo ufw status verbose
```

**Likely cause:** Port 80 or 443 is not allowed.

**Corrective action:**
```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

**Verification:**
```bash
curl -sf http://school.example.lan
```

---

## Appendix: Quick Reference

### A.1 Common Docker Commands

```bash
# Start all services
cd /opt/sms
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env up -d

# Stop all services
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env down

# View logs
sudo -u sms_admin docker logs sms-api
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env logs -f

# Restart a service
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env restart api

# Run database migration
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll migrate-database

# Run database seed
sudo -u sms_admin docker compose -f docker/docker-compose.prod.yml --env-file env/.env run --rm api dotnet SMS.API.dll seed-data

# Check container health
docker inspect --format='{{.State.Health.Status}}' sms-api

# Check all running containers
docker ps
```

### A.2 Important File Paths

| File | Path |
|------|------|
| Production Docker Compose | `/opt/sms/docker/docker-compose.prod.yml` |
| Base Docker Compose | `/opt/sms/docker/docker-compose.yml` |
| Nginx Configuration | `/opt/sms/docker/nginx.conf` |
| Prometheus Configuration | `/opt/sms/docker/prometheus.yml` |
| Prometheus Alert Rules | `/opt/sms/docker/prometheus-alerts.yml` |
| Alertmanager Configuration | `/opt/sms/docker/alertmanager.yml` |
| Grafana Datasource Config | `/opt/sms/docker/grafana-datasources/datasource.yml` |
| Grafana Dashboard Provider | `/opt/sms/docker/grafana-dashboards/dashboard-provider.yml` |
| Environment File | `/opt/sms/env/.env` |
| Environment Example | `/opt/sms/.env.example` |
| TLS Certificate | `/etc/ssl/certs/school.example.lan.crt` |
| TLS Private Key | `/etc/ssl/private/school.example.lan.key` |
| Deployment Script | `/opt/sms/scripts/deploy.sh` |
| Backup Script | `/opt/sms/scripts/backup.sh` |
| Restore Script | `/opt/sms/scripts/restore.sh` |
| Migration Script | `/opt/sms/scripts/migrate.sh` |
| Seed Script | `/opt/sms/scripts/seed.sh` |
| Health Check Script | `/opt/sms/scripts/health-check.sh` |

### A.3 Port Reference

| Port | Service | Docker Container | External Access |
|------|---------|-----------------|-----------------|
| 22 | SSH | Host | Administrators |
| 80 | HTTP | Nginx | All LAN clients |
| 443 | HTTPS | Nginx | All LAN clients |
| 5433 | PostgreSQL | sms-postgres | 🔒 Do not expose |
| 5000 | API | sms-api | 🔒 Do not expose |
| 3000 | Frontend | sms-web | 🔒 Do not expose |
| 3001 | Grafana | sms-grafana | Administrators |
| 9090 | Prometheus | sms-prometheus | Administrators |
| 9093 | Alertmanager | sms-alertmanager | Administrators |
| 9100 | Node Exporter | sms-node-exporter | 🔒 Do not expose |
| 9187 | Postgres Exporter | sms-postgres-exporter | 🔒 Do not expose |
| 8080 | cAdvisor | sms-cadvisor | 🔒 Do not expose |

---

> **End of Guide**
>
> This document was generated by scanning the actual School Management System project files.
> Last updated: August 2026
> Target: Debian 13 "Trixie" — Production Server Preparation
