namespace SMS.Domain.Enums
{
    /// <summary>
    /// Type of an OMS road account.
    /// NOTE: the meaning of road accounting types is an open requirement
    /// (Documentation/OMS/OMS_OPEN_REQUIREMENTS.md #18); this initial set is a
    /// development default and no accounting formula is implemented in Phase 2.
    /// </summary>
    public enum RoadAccountType
    {
        /// <summary>A road-associated account.</summary>
        Road = 1,

        /// <summary>Any other account type pending business confirmation.</summary>
        Other = 2
    }
}
