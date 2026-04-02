# Self-hosted PostgreSQL with Dokploy-managed backups

**Status**: Accepted (updated 2026-04-02)

## Decision

PostgreSQL is self-hosted as a Docker Compose service on the OVH VPS. Backups are managed via Dokploy's built-in database backup feature, which uses `pg_dump` internally and supports cron scheduling, retention policies, and S3-compatible storage destinations. Managed PostgreSQL (OVH DBaaS) is not used.

## Reason

OVHCloud managed PostgreSQL starts at approximately €44/month — too expensive for an early-stage product with no revenue. Self-hosting on the same VPS eliminates this cost. Dokploy provides a built-in backup feature for database services that eliminates the need for custom cron scripts.

## Backup strategy (to be implemented in Phase 0)

- Configure Dokploy database backup via the dashboard Backups tab
- Schedule: daily via cron expression
- Destination: OVHCloud Object Storage bucket (S3-compatible, EU region)
- Retention: 30 backups
- Monthly restore drill to verify backup integrity
- Monitor backup status via Dokploy dashboard notifications
