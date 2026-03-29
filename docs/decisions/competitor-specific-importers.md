# No generic CSV import; competitor-specific importers only

**Status**: Accepted

## Decision

No generic CSV/Excel import. Instead build Holdsport and MinForening-specific importers, plus an invitation flow.

## Reason

Target users (volunteer formænd, kasserere) are not technical enough to reliably produce a correctly formatted CSV. Competitor importers require only one upload step. Invitation flow requires no prior data at all.
