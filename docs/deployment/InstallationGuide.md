# Installation Guide

## Overview

This guide provides step-by-step instructions for installing the School Management System in a production environment.

## Prerequisites

### Hardware Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| CPU | 8 Cores | 16+ Cores |
| RAM | 16 GB | 32+ GB |
| Storage | 500 GB SSD | 1+ TB SSD |
| Network | 1 Gbps | 10 Gbps |

### Software Requirements

- Docker 24.0+
- Docker Compose 2.20+
- Git 2.40+
- OpenSSL 3.0+ (for SSL certificates)

### Operating Systems Supported

- Ubuntu Server 22.04 LTS (Recommended)
- Windows Server 2022
- CentOS 8 / RHEL 8

## Installation Steps

### 1. System Preparation

#### Ubuntu Server

```bash
# Update system
sudo apt update && sudo apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh

# Install Docker Compose
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Install Git
sudo apt install git -y

# Install OpenSSL
sudo apt install openssl -y