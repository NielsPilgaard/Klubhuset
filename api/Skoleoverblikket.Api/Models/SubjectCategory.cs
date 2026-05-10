namespace Skoleoverblikket.Api.Models;

/// <summary>
/// UVM-aligned subject category. Stored as integer; do not reorder or renumber existing values.
/// Fri = free/custom (school-specific courses not mapped to a UVM subject).
/// Enum member names are ASCII-safe — Danish display names live in the frontend.
/// </summary>
public enum SubjectCategory
{
    Dansk                 = 0,
    Matematik             = 1,
    Engelsk               = 2,
    Naturfag              = 3,
    Historie              = 4,
    Musik                 = 5,
    Idraet                = 6,  // Idræt
    Kristendomskundskab   = 7,
    Billedkunst           = 8,
    HaandvaerkOgDesign    = 9,  // Håndværk og design
    Tysk                  = 10,
    Fransk                = 11,
    Geografi              = 12,
    Biologi               = 13,
    FysikKemi             = 14,
    Samfundsfag           = 15,
    Fri                   = 16,
}
