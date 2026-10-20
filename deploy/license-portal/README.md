# License portal deploy pack

Docker Compose stack: **Caddy** (TLS) + **ASP.NET LicensePortal** + **Postgres**.

Full ops guide: [docs/LICENSE-PORTAL.md](../../docs/LICENSE-PORTAL.md).

```bash
cp .env.example .env   # edit domain, passwords, admin
# place private-key.pem + public-key.pem under data/keys/
docker compose up -d --build
```

Public update feed: `https://$PORTAL_DOMAIN/updates/feed.json`
