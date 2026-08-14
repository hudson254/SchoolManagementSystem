using System;
using System.Diagnostics.CodeAnalysis;

namespace SMS.Application.Common
{
    /// <summary>
    /// Helpers for validating and working with UUID public identifiers.
    ///
    /// Architecture note:
    /// In this system, entity primary keys are already non-sequential Guids
    /// (<c>BaseEntity.Id</c>). That Guid IS the public identifier used in:
    ///   - REST API route parameters
    ///   - Request/response DTOs
    ///   - Frontend routes and state
    ///   - Cross-service / internal API calls
    ///   - Certificate and report verification references (where applicable)
    ///
    /// Rules:
    /// 1. Never expose sequential integer database IDs through APIs.
    /// 2. Never treat possession of a UUID as authorization.
    /// 3. Always enforce tenant isolation + role/ownership checks after resolving a PublicId.
    /// 4. PublicIds are immutable after creation and must not be client-supplied on create
    ///    unless there is an explicit architectural requirement.
    /// 5. Reject empty / malformed UUIDs before they reach the database layer.
    /// </summary>
    public static class PublicId
    {
        /// <summary>
        /// Returns true when <paramref name="value"/> is a non-empty Guid suitable for use as a public identifier.
        /// </summary>
        public static bool IsValid(Guid value) => value != Guid.Empty;

        /// <summary>
        /// Attempts to parse a string as a non-empty Guid public identifier.
        /// </summary>
        public static bool TryParse([NotNullWhen(true)] string? input, out Guid publicId)
        {
            publicId = Guid.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (!Guid.TryParse(input, out var parsed))
                return false;

            if (parsed == Guid.Empty)
                return false;

            publicId = parsed;
            return true;
        }

        /// <summary>
        /// Parses a string as a non-empty Guid public identifier.
        /// Throws <see cref="ArgumentException"/> when the value is missing, malformed, or empty.
        /// </summary>
        public static Guid ParseRequired(string? input, string parameterName = "id")
        {
            if (!TryParse(input, out var publicId))
            {
                throw new ArgumentException(
                    $"'{parameterName}' must be a valid non-empty UUID public identifier.",
                    parameterName);
            }

            return publicId;
        }

        /// <summary>
        /// Ensures a Guid public identifier is non-empty.
        /// Throws <see cref="ArgumentException"/> when empty.
        /// </summary>
        public static Guid EnsureValid(Guid value, string parameterName = "id")
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException(
                    $"'{parameterName}' must be a non-empty UUID public identifier.",
                    parameterName);
            }

            return value;
        }

        /// <summary>
        /// Generates a new cryptographically suitable UUID public identifier.
        /// Prefer letting <c>BaseEntity</c> generate Ids automatically; use this only when
        /// an Id must be allocated before the entity is constructed.
        /// </summary>
        public static Guid New() => Guid.NewGuid();
    }
}
