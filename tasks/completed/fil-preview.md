# Task: Fil preview

## Problem
Files in FilesPage and WeekPlanPage can only be downloaded. There is no inline preview, so users must download to know what a file contains — especially annoying on mobile.

## Desired behavior
Clicking a file name (or a preview icon) opens a preview modal:
- **Images** (image/*): render inline with `<img>`
- **PDF** (application/pdf): render with `<iframe src={url}>` or an `<embed>` tag
- **Other types**: show file name, size, type, and a download button — no preview possible

## Scope
- New component: `FilePreviewModal` (new file or inline in FilesPage)
- Add preview trigger to each row in FilesPage table (click file name or eye icon)
- Consider also wiring into WeekPlanPage slot file list

## Notes
- Files are served from S3 with presigned URLs — check CORS headers allow inline display (Content-Disposition: inline vs attachment). May need API change to control this.
- PDF iframe preview works cross-origin only if the S3 bucket allows it. Fallback to download link if not.
- Keep it simple: no react-pdf dependency needed if iframe works.

## Files affected
- `web/src/pages/FilesPage.tsx`
- New: `web/src/components/FilePreviewModal.tsx` (or inline)
- Possibly: API presign endpoint — add `inline: true` param to set `Content-Disposition: inline`
