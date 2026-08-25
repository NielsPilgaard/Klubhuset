using System.Text.RegularExpressions;

namespace Skoleoverblikket.Api.Services;

/// <summary>Shared phone/postal-code normalization for parent and staff contact info.</summary>
public static partial class ContactValidation
{
	[GeneratedRegex(@"^(\+45)?\d{8}$")]
	private static partial Regex PhoneRegex();

	[GeneratedRegex(@"^\d{4}$")]
	private static partial Regex PostalCodeRegex();

	/// <summary>Strips whitespace and validates. Empty/null input is valid (field is optional).</summary>
	public static bool TryNormalizePhone(string? input, out string? normalized)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			normalized = null;
			return true;
		}

		var stripped = WhitespaceRegex().Replace(input, string.Empty);
		if (!PhoneRegex().IsMatch(stripped))
		{
			normalized = null;
			return false;
		}

		normalized = stripped;
		return true;
	}

	public static bool TryNormalizePostalCode(string? input, out string? normalized)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			normalized = null;
			return true;
		}

		var trimmed = input.Trim();
		if (!PostalCodeRegex().IsMatch(trimmed))
		{
			normalized = null;
			return false;
		}

		normalized = trimmed;
		return true;
	}

	[GeneratedRegex(@"\s+")]
	private static partial Regex WhitespaceRegex();
}
