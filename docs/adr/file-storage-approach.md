# File storage via OVHCloud Object Storage

**Status**: Accepted

## Decision

Files uploaded by schools (course materials, documents, PDFs) are stored in OVHCloud Object Storage (S3-compatible API). Files are linked to courses and browsable via a file explorer in the app.

## Reason

OVHCloud Object Storage is already in the stack (used for database backups), EU-hosted, and cost-effective. Using the S3-compatible API means standard tooling works (AWS SDK for .NET). Storage quotas per tier (100 GB Basis, 1000 GB Skole+) provide a natural upgrade lever tied to actual cost.
