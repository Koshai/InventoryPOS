# Internal license portal (VPS)

Staff-only web dashboard for issuing InventoryPOS licenses and publishing desktop updates. Customers never log in; they still import `.iposlic` files and upgrade via Settings.

## What it provides

- Issue / renew RSA schema-2 licenses (email + Installation ID) using the same [`LicenseIssuerService`](../src/InventoryPOS.Licensing.Issuer/LicenseIssuerService.cs) as License Manager
- Customer / machine / issue history in Postgres
- Upload Inno `InventoryPOSSetup.exe`, compute SHA-256, publish public `GET /updates/feed.json`
- Invite-only staff accounts (Admin / Issuer). Public registration is disabled.

Keep the WPF **License Manager** as an offline emergency issuer (air-gapped laptop + local keys).

## VPS stack

| Piece | Choice |
|-------|--------|
| OS | Ubuntu 24.04 LTS |
| Packaging | Docker Compose under [`deploy/license-portal/`](../deploy/license-portal/) |
| TLS | Caddy (Let’s Encrypt) |
| DB | Postgres 16 in Compose |
| App | `tools/InventoryPOS.LicensePortal` |
| Keys | Host-mounted `data/keys/*.pem` (read-only in container) |

Recommended providers: Linode Nanode/shared 2GB, Hetzner CX22, or DigitalOcean droplet.

### No domain yet

Let’s Encrypt needs a hostname. Until you buy a domain, use the Linode **public IP over HTTP**:

```bash
cp Caddyfile.ip Caddyfile
# .env:
#   PUBLIC_BASE_URL=http://YOUR.LINODE.IP
#   PORTAL_DOMAIN=unused.local   # ignored by Caddyfile.ip
```

Staff UI: `http://YOUR.LINODE.IP`  
Update feed: `http://YOUR.LINODE.IP/updates/feed.json`  

This is fine for internal testing. Add a domain + switch back to the default `Caddyfile` later for HTTPS.

## First deploy (Linode)

1. Create a Linode: **Ubuntu 24.04 LTS**, shared 2GB is enough. Note the public IPv4.
2. In Linode Firewall (or `ufw`): allow **22, 80**, and **443** (443 unused until you have a domain). Prefer SSH keys.
3. SSH in and install Docker:

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo $VERSION_CODENAME) stable" | sudo tee /etc/apt/sources.list.d/docker.list
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin git
sudo usermod -aG docker $USER   # then log out/in
```

4. Clone the repo (full tree needed for the Docker build context):

```bash
git clone https://github.com/Koshai/InventoryPOS.git
cd InventoryPOS/deploy/license-portal
cp .env.example .env
cp Caddyfile.ip Caddyfile   # skip this if you already have a domain
nano .env                   # set PUBLIC_BASE_URL=http://YOUR.IP and passwords
```

5. Keys (same pair as Desktop `TrustedLicenseKeys`):

```bash
mkdir -p data/keys
# from your admin PC:
# scp private-key.pem public-key.pem user@YOUR.IP:~/InventoryPOS/deploy/license-portal/data/keys/
chmod 600 data/keys/private-key.pem
chmod 644 data/keys/public-key.pem
```

6. Start:

```bash
docker compose up -d --build
```

7. Open `http://YOUR.IP`, log in with bootstrap admin, **change the password**.

8. Point Desktop `Updates:FeedUrl` at `http://YOUR.IP/updates/feed.json` when you want online updates (optional for licensing).

## Host on a Mac (LAN, no domain)

Use this for internal testing while the Mac is on and awake. Other PCs on the same Wi‑Fi/LAN reach the portal by the Mac’s local IP.

### 1. Install Docker Desktop

1. Install [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop/).
2. Open Docker Desktop and wait until it says **Engine running**.
3. Apple Silicon (M1/M2/M3) is fine; Compose uses multi-arch images.

### 2. Find the Mac’s LAN IP

In Terminal:

```bash
ipconfig getifaddr en0
```

If that is empty (Ethernet instead of Wi‑Fi), try `en1` or:

```bash
ifconfig | grep "inet " | grep -v 127.0.0.1
```

You want something like `192.168.1.50` (not `127.0.0.1`). Prefer a **DHCP reservation** or static IP in your router so this address does not change.

### 3. Keep the Mac reachable

- **System Settings → Battery / Energy**: disable sleep when on power, or set “Prevent automatic sleeping when display is off” while hosting.
- **System Settings → Network → Firewall**: if the firewall is on, allow **Docker** / incoming connections on port **80**, or temporarily turn the firewall off for a quick test.
- Mac must stay **powered on** and on the same network as staff PCs.

### 4. Get the code and keys

```bash
git clone https://github.com/Koshai/InventoryPOS.git
cd InventoryPOS/deploy/license-portal

cp .env.example .env
cp Caddyfile.ip Caddyfile

mkdir -p data/keys
# Copy your LicenseIssuer PEMs into data/keys/ (same pair as Desktop TrustedLicenseKeys):
#   private-key.pem
#   public-key.pem
chmod 600 data/keys/private-key.pem
chmod 644 data/keys/public-key.pem
```

If keys still live only on your Windows PC:

```text
%ProgramData%\InventoryPOS\LicenseIssuer\private-key.pem
%ProgramData%\InventoryPOS\LicenseIssuer\public-key.pem
```

Copy those two files to the Mac under `deploy/license-portal/data/keys/`.

### 5. Edit `.env`

Replace `192.168.1.50` with the IP from step 2:

```env
PORTAL_DOMAIN=unused.local
PUBLIC_BASE_URL=http://192.168.1.50

POSTGRES_PASSWORD=pick-a-long-random-string

BOOTSTRAP_ADMIN_EMAIL=admin@yourcompany.com
BOOTSTRAP_ADMIN_PASSWORD=ChangeMe!Portal1
```

Use `http://` (not `https://`). `PUBLIC_BASE_URL` must match the IP others will type in the browser.

### 6. Start the portal

```bash
cd ~/InventoryPOS/deploy/license-portal   # or your clone path
docker compose up -d --build
```

First build can take several minutes. Check status:

```bash
docker compose ps
docker compose logs -f portal
```

### 7. Open and log in

On the Mac or any PC on the same LAN:

- Portal: `http://192.168.1.50` (your IP)
- Login with `BOOTSTRAP_ADMIN_EMAIL` / `BOOTSTRAP_ADMIN_PASSWORD`
- Change the password after first login
- Issue licenses as usual; email `.iposlic` to customer PCs

Optional updates feed later:

```json
"Updates": { "FeedUrl": "http://192.168.1.50/updates/feed.json" }
```

in `%ProgramData%\InventoryPOS\config\appsettings.Local.json` on Windows clients.

### 8. Day-to-day commands

```bash
cd ~/InventoryPOS/deploy/license-portal
docker compose up -d          # start
docker compose down           # stop
docker compose logs -f        # watch logs
docker compose up -d --build  # after git pull / code changes
```

### Access from outside your home/office

The Mac LAN IP is **not** reachable from the public internet. Options:

1. **Tailscale** (recommended): install Tailscale on the Mac and on staff PCs; use the Mac’s Tailscale IP in `PUBLIC_BASE_URL` and rebuild/restart Compose after changing `.env`.
2. Router port-forward of WAN → Mac:80 (more risk; avoid unless you know the tradeoffs).
3. Move to Linode later using the same `deploy/license-portal` folder.

### Local development (optional)

- Postgres reachable with the connection string in `appsettings.json`
- Set `Portal:BootstrapAdminPassword` in user secrets or `appsettings.Development.json`
- Place keys under `tools/InventoryPOS.LicensePortal/App_Data/keys/`
- `dotnet run --project tools/InventoryPOS.LicensePortal`

## Desktop update feed

Public (no auth):

- `GET https://<domain>/updates/feed.json`
- `GET https://<domain>/updates/InventoryPOSSetup-{version}.exe`

Feed shape matches [`UPDATE-FEED.example.json`](UPDATE-FEED.example.json).

Set on each machine (bundled config or ProgramData override):

```json
"Updates": {
  "FeedUrl": "https://licenses.yourdomain.com/updates/feed.json"
}
```

Then Settings → **Check for updates online** → **Download and install update**.

## Publishing a release

1. Bump version in `InventoryPOS.Desktop.csproj` and `installer/InventoryPOS.iss`.
2. Build setup (`installer/scripts/Build-Publish.ps1` + Inno).
3. Portal → **Releases** → upload the `.exe`, set version, mark current.
4. Clients with `FeedUrl` set will see the new version.

## Backups

Nightly (cron on the VPS):

```bash
# Postgres dump
docker compose exec -T postgres pg_dump -U portal inventorypos_license_portal \
  | gzip > /var/backups/inventorypos-portal-$(date +%F).sql.gz

# Keys + releases (issued licenses are regenerable but useful to keep)
tar czf /var/backups/inventorypos-portal-files-$(date +%F).tgz \
  -C /path/to/deploy/license-portal data/keys \
  && docker run --rm -v license-portal_portal_releases:/data -v /var/backups:/out alpine \
     tar czf /out/inventorypos-releases-$(date +%F).tgz -C /data .
```

Copy `/var/backups` off-box. Never commit `private-key.pem` or `.env`.

## Security checklist

- [ ] SSH key-only; `ufw allow OpenSSH; ufw allow 80; ufw allow 443; ufw enable`
- [ ] Unattended security updates on Ubuntu
- [ ] Bootstrap password changed; only invite trusted staff
- [ ] Private key `chmod 600`, volume mounted `:ro`
- [ ] Public endpoints are feed + installer only; admin UI requires login
- [ ] Encrypted offline backup of `private-key.pem`

## Ops scripts

See [`deploy/license-portal/scripts/`](../deploy/license-portal/scripts/) for backup helpers.
