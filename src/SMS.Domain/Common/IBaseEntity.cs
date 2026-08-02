namespace SMS.Domain.Common
{
    /// <summary>
    /// Interface defining the contract for base entity properties
    /// </summary>
    public interface IBaseEntity
    {
        Guid Id { get; set; }
        Guid TenantId { get; set; }
        string CreatedBy { get; set; }
        DateTime CreatedDate { get; set; }
        string? ModifiedBy { get; set; }
        DateTime? ModifiedDate { get; set; }
        string? DeletedBy { get; set; }
        DateTime? DeletedDate { get; set; }
        bool IsDeleted { get; set; }
        byte[]? RowVersion { get; set; }
    }
}