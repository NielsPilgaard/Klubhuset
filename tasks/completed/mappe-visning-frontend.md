# Task: Mappe visning — frontend integration

## Problem
The API fully supports folders (`FolderDto`, `CreateFolderRequest`, `RenameFolderRequest`, CRUD endpoints at `/api/v1/files/folders`). The frontend `FilesPage` is a flat list with no folder UI.

## API endpoints already available
- `POST /api/v1/files/folders` — create folder
- `DELETE /api/v1/files/folders/{id}` — delete folder
- `PATCH /api/v1/files/folders/{id}` — rename folder
- `GET /api/v1/files` — already returns `folderId` on each `FileDto` and presumably supports `?folderId=` filter (confirm)

## Desired behavior
1. **Folder breadcrumb navigation** — header shows current path (e.g. "Filer / Matematik / Uge 10")
2. **Folder listing** — folders shown above files in the table with a folder icon; click to navigate in
3. **Create folder button** — admin only, opens a small name input inline or modal
4. **Rename folder** — inline rename on double-click or pencil icon (admin only)
5. **Delete folder** — with confirmation; only if empty, or cascade if API supports it
6. **Upload into folder** — `UploadModal` uses current `folderId` (already wired via `currentFolderId` prop which is currently always `null`)

## Scope
Large. New state management for current folder ID, breadcrumb trail, folder list. Recommend tackling in one PR.

## Files affected
- `web/src/pages/FilesPage.tsx` — significant rewrite
- `web/src/api/generated/` — generated client should already have folder endpoints; verify after regenerating
