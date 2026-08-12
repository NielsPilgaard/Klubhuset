---
title: 'Reply-to for Beskeder messages'
purpose: 'Scope a reply/thread-continuation feature for the Beskeder (Messages) inbox — currently absent from the data model and UI.'
description: >-
  Users cannot reply to a message they sent themselves to continue a
  conversation. No reply/thread concept exists yet anywhere in the Messages
  feature — this is new feature work, not a bug fix.
status: 'Proposed'
---

# Reply-to for Beskeder messages

## TL;DR

`Message` has no `InReplyToId`/thread field, `MessagesController.cs` has no
reply endpoint, and `BeskederPage.tsx` has no reply button — confirmed by
full-repo search. Building reply-to requires: a thread/parent field on the
data model, a reply endpoint, and reply UI on the message detail panel.
Sized as its own task, not a quick fix.

## Context

Current behavior: `BeskederPage.tsx` renders `selectedMsg` (message detail
panel) with no action buttons — only the compose modal
(`handleOpenCompose(prefilledRecipient?)`) can start a new message, and only
from the contacts/directory panel via `handleDirectoryContactClick`. Sent and
inbox messages are unrelated rows; nothing links a reply to the message it's
replying to, so users lose thread context on their own sent messages when
they try to continue a conversation.

Note: the separate "Kontaktbog" feature (`ContactThreadsController.cs`,
`ContactMessage.cs`) already has a per-student thread model
(`FindOrCreateThread` / `AddMessage`), but it's a different feature (parent↔
teacher per-child messaging) and not a template to copy directly — Beskeder
is a flat inbox across all tenant users.

## Proposed scope

1. **Data model**: add `InReplyToId Guid?` (self-referencing FK) to
   `Message` (`api/Skoleoverblikket.Api/Models/Message.cs`). New EF Core
   migration via `/add-migration` — never edit existing migrations.
2. **Backend**: `MessagesController.cs` — accept `InReplyToId` on send
   (`POST /api/v1/messages`), validate the parent message belongs to a
   thread the caller can see (sent or received by them), and include it in
   `InboxMessageDto`/`SentMessageDto` responses. Decide how a "thread"
   is defined for grouping (e.g. reuse `InReplyToId` chain vs. a separate
   `ThreadId`) — needs a decision before implementation.
3. **Frontend**: `BeskederPage.tsx` — add a "Svar" (reply) action on the
   message detail panel (works for messages the user sent *and* received),
   prefill compose with recipient + `InReplyToId`, and render thread
   continuation (e.g. group by thread in the sent/inbox lists, or show a
   "previous message" reference).
4. Regenerate typed API client (`/codegen`) after controller/DTO changes.

## Open questions

- Does "reply to own sent message" mean replying to the *recipient* of that
  message (continuing the conversation with them), or something else? Needs
  confirmation before implementation — current wording assumes the former. Answer: It means replying to the recipient, but with the last sent email in the thread still.
- Group messages (`GroupMessageId`) complicate reply-to: replying to one
  fan-out row of a group send should probably reply only to that one
  recipient, not re-broadcast to the whole group. Needs explicit handling. Answer: No support for replying to group messages, we cannot assume rights
