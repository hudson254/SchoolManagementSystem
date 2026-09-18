using System;

namespace SMS.Domain.Common
{
    /// <summary>
    /// Centralized monetary helpers for OMS (development defaults per
    /// Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #16-#17):
    /// decimal arithmetic only (never float/double), currency scale 2,
    /// half-even ("banker's") rounding. Change here when the confirmed
    /// business rule arrives — no order-model rewrite needed.
    /// </summary>
    public static class OmsMoney
    {
        /// <summary>Decimal places used for OMS monetary values (decimal(18,2)).</summary>
        public const int CurrencyDecimalPlaces = 2;

        /// <summary>Default ISO-4217 currency code (open requirement #16).</summary>
        public const string DefaultCurrency = "KES";

        /// <summary>Rounds a monetary value to the currency scale using half-even rounding.</summary>
        public static decimal Round(decimal value) =>
            Math.Round(value, CurrencyDecimalPlaces, MidpointRounding.ToEven);

        /// <summary>Computes an order-item line total: quantity × unit price, rounded to scale.</summary>
        public static decimal LineTotal(int quantity, decimal unitPrice) =>
            Round(quantity * unitPrice);
    }
}
