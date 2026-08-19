# Test

1. Test ferietilmelding (parent side, onboard new parent into class with schema and other fake parents, login as that parent)
2. Test kontaktbog (parent side, onboard new parent into class with schema and other fake parents, login as that parent)
3. Test fravær (parent side, onboard new parent into class with schema and other fake parents, login as that parent)
4. Test ugeplan print (parent side, onboard new parent into class with schema and other fake parents, login as that parent)
5. Test that notif dropdown looks good
6. Check landing page pricings
7. Check sub page pricings
8. Check Stripe pricing (yearly toggle working?)
9. Verify task 39 output
   - Calendar grid parity: parent login → calendar page shows month grid/carousel (not flat list). .ics export works.
   - View-as-parent: superadmin "view as parent" on /calendar renders correctly.
   - Parent profile page (foraeldrevisning/profil): edit Name/Phone/Address/PostalCode/City/ShareContactInfo, save succeeds.
   - GET /parents/me returns new fields after save, persists on refresh.
   - Admin EditContactModal for parents still works unchanged.