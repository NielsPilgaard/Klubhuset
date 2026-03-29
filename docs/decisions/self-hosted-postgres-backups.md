# Self-hosted PostgreSQL with manual backup strategy

**Status**: Accepted

## Decision

PostgreSQL is self-hosted as a Docker Compose service on the OVH VPS. Backups are performed via `pg_dump` on a cron schedule and uploaded to OVHCloud Object Storage. Managed PostgreSQL (OVH DBaaS) is not used.

## Reason

OVHCloud managed PostgreSQL starts at approximately €44/month — too expensive for an early-stage product with no revenue. Self-hosting on the same VPS eliminates this cost. The tradeoff is that backup, monitoring, and restore procedures must be owned by the developer.

## Backup strategy (to be implemented in Phase 0)

- Daily `pg_dump` → gzip → upload to OVHCloud Object Storage bucket (EU region)
- Retain last 30 daily backups
- Monthly restore drill to verify backup integrity
- Alert on backup failure (simple health-check script or Dokploy notification)
