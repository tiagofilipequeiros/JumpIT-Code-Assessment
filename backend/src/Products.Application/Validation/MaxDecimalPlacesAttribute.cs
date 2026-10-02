using System.ComponentModel.DataAnnotations;

namespace Products.Application.Validation;

// Rejects values with more decimal places than the database stores (instead of silently rounding).
public class MaxDecimalPlacesAttribute(int places) : ValidationAttribute($"The field {{0}} can have at most {places} decimal places.")
{
    public override bool IsValid(object? value) =>
        value is not decimal number || decimal.Round(number, places) == number;
}
