# Stripe Checkout for self-serve billing

**Status**: Accepted

## Decision

All billing is handled via Stripe Checkout. Schools sign up, enter card details, and are billed monthly with auto-renew. The Stripe billing portal is used for subscription management (upgrade, downgrade, cancel). No MobilePay integration. No manual invoicing.

## Reason

Schools have limited time and limited admin capacity. Self-serve billing with auto-renew eliminates manual invoicing burden for both the school and the platform operator. Stripe Checkout is well-supported, handles SCA/3DS, and provides a billing portal out of the box. MobilePay adds complexity without meaningful value for a B2B monthly subscription.
