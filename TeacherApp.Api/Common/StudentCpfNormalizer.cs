using System.Linq;

namespace TeacherApp.Api.Common;

public static class StudentCpfNormalizer
{
    public static string ToDigits(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return "";

        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    public static bool IsValidBrazilianCpfDigits(string digitsOnly)
    {
        if (digitsOnly.Length != 11)
            return false;

        if (digitsOnly.Distinct().Count() == 1)
            return false;

        return ValidateCheckDigits(digitsOnly);
    }

    private static bool ValidateCheckDigits(string cpf)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
            sum += (cpf[i] - '0') * (10 - i);
        var mod = sum % 11;
        var d1 = mod < 2 ? 0 : 11 - mod;
        if (cpf[9] - '0' != d1)
            return false;

        sum = 0;
        for (var i = 0; i < 10; i++)
            sum += (cpf[i] - '0') * (11 - i);
        mod = sum % 11;
        var d2 = mod < 2 ? 0 : 11 - mod;
        return cpf[10] - '0' == d2;
    }
}
