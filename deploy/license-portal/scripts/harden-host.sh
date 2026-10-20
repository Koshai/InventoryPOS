#!/usr/bin/env bash
# Optional one-time host hardening helpers for Ubuntu. Review before running.
set -euo pipefail

if [[ "$(id -u)" -ne 0 ]]; then
  echo "Run as root (sudo)."
  exit 1
fi

apt-get update
apt-get install -y ufw fail2ban unattended-upgrades

ufw default deny incoming
ufw default allow outgoing
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable

dpkg-reconfigure -f noninteractive unattended-upgrades

echo "Firewall and unattended-upgrades configured. Prefer SSH keys; disable PasswordAuthentication in /etc/ssh/sshd_config."
