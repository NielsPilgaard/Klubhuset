# UVM/STIL Minimumstimetal Reporting

Folkeskoler are legally required (Folkeskoleloven §14b) to document that each class receives the minimum annual hours per subject. Schools report this to their municipality, which forwards it to STIL. There is no live STIL push API — schools submit a structured extract (CSV or Excel) that the municipality ingests.

Skoleoverblikket already tracks hours per course per class (the Stats feature). This task turns that data into a compliant export.

## Report structure

Per school year, per class, per subject (fag):
- Scheduled hours per week
- Annual total (hours × weeks)
- Legal minimum (from UVM's published minimumstimetal table)
- Status: meets minimum / shortfall

## Tasks

- [ ] Look up and embed UVM's current minimumstimetal table (subject × klassetrin → minimum annual hours)
- [ ] Add school-year concept to scheduled hours calculation (vs. current per-week view)
- [ ] Generate CSV/Excel export: klasse × fag × scheduled hours × minimum × status
- [ ] Traffic-light indicator in the UI: green (meets minimum), yellow (close), red (shortfall)
- [ ] Export is scoped per tenant and per school year
