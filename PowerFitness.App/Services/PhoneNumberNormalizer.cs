namespace PowerFitness.App.Services;

public static class PhoneNumberNormalizer
{
    public static string Normalize(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits.StartsWith('8'))
        {
            digits = $"7{digits[1..]}";
        }
        else if (digits.Length == 10)
        {
            digits = $"7{digits}";
        }

        return digits.Length == 0 ? string.Empty : $"+{digits}";
    }
}
