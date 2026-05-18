# Task: Omdøb fil (rename during upload)

## Problem
Once a file is uploaded, its display name is fixed to whatever the file system called it (e.g. `IMG_20240301_142233.jpg`). Users cannot give it a meaningful name.

## Desired behavior
In the `UploadModal`, after selecting a file, show an editable "Filnavn" text input pre-filled with `selectedFile.name` (without extension). User can change it before uploading. The chosen name is sent as `fileName` in the presign request body.

## Scope
- `FilesPage.tsx` — `UploadModal`: add controlled text input for file name, keep extension locked or appended automatically
- Pass the user-edited name (with original extension appended) to `postApiV1FilesPresign({ body: { fileName: editedName, ... } })`
- No API change needed — `fileName` field already exists in `PresignFileRequest`

## UX detail
- Show input only after file is selected (not before)
- Strip extension from the input display; re-append on submit (so user edits `rapport Q1` not `rapport Q1.pdf`)
- Keep file size display next to the name

## Files affected
- `web/src/pages/FilesPage.tsx` (UploadModal component, lines 83–225)
