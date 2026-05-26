using System.Globalization;

namespace Skoleoverblikket.Api.Controllers;

internal static class IsoWeekValidation
{
	internal static bool IsValid(int isoYear, int isoWeek) =>
		isoYear >= 2020 && isoYear <= 2100 &&
		isoWeek >= 1 && isoWeek <= ISOWeek.GetWeeksInYear(isoYear);
}
