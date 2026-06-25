#!/bin/bash
# Run this script on a fresh Scaleway instance to set up the Nakama backend.
# Tested on Ubuntu 24.04 LTS.
#
# Usage:
#   scp -r ../Backend ubuntu@<your-instance-ip>:/opt/nakama-backend
#   ssh ubuntu@<your-instance-ip>
#   cd /opt/nakama-backend && bash deploy/scaleway-setup.sh

set -euo pipefail

echo "==> Updating system packages"
apt-get update -y && apt-get upgrade -y

echo "==> Installing Docker"
apt-get install -y ca-certificates curl gnupg
install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
chmod a+r /etc/apt/keyrings/docker.gpg
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" \
  > /etc/apt/sources.list.d/docker.list
apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

echo "==> Enabling Docker service"
systemctl enable docker
systemctl start docker

echo "==> Verifying .env file exists"
if [ ! -f .env ]; then
  echo "ERROR: .env file not found. Copy .env.example to .env and fill in your CockroachDB credentials."
  exit 1
fi

echo "==> Pulling Nakama image"
docker compose -f docker-compose.prod.yml pull

echo "==> Starting Nakama (production)"
docker compose -f docker-compose.prod.yml up -d

echo ""
echo "==> Done. Nakama is running."
echo "    API:     http://$(curl -s ifconfig.me):7349"
echo "    Socket:  http://$(curl -s ifconfig.me):7350"
echo "    Console: http://$(curl -s ifconfig.me):7351"
