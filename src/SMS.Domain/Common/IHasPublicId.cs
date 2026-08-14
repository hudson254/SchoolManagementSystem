using System;

namespace SMS.Domain.Common
{
    // IHasPublicId is defined in BaseEntity.cs alongside IBaseEntity.
    // This file is intentionally left as a pointer for discoverability.
    //
    // Public identifier strategy:
    //   - BaseEntity.Id (Guid) is both the database primary key and the public API identifier.
    //   - Guids are non-sequential and generated via Guid.NewGuid() at construction.
    //   - APIs, DTOs, frontend routes, certificates, and internal service calls use this Guid.
    //   - Possession of a UUID is never authorization; tenant + role checks always apply.
}
