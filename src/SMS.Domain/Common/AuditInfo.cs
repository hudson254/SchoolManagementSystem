namespace SMS.Domain.Common
{
    public class AuditInfo : ValueObject
    {
        public string CreatedBy { get; private set; }
        public DateTime CreatedDate { get; private set; }
        public string? ModifiedBy { get; private set; }
        public DateTime? ModifiedDate { get; private set; }
        public string? DeletedBy { get; private set; }
        public DateTime? DeletedDate { get; private set; }
        public bool IsDeleted { get; private set; }

        private AuditInfo()
        {
            CreatedBy = "SYSTEM";
            CreatedDate = DateTime.UtcNow;
        }

        public AuditInfo(string createdBy) : this()
        {
            CreatedBy = createdBy;
            CreatedDate = DateTime.UtcNow;
        }

        public AuditInfo(string createdBy, DateTime createdDate) : this(createdBy)
        {
            CreatedDate = createdDate;
        }

        public void MarkModified(string modifiedBy)
        {
            ModifiedBy = modifiedBy;
            ModifiedDate = DateTime.UtcNow;
        }

        public void MarkDeleted(string deletedBy)
        {
            IsDeleted = true;
            DeletedBy = deletedBy;
            DeletedDate = DateTime.UtcNow;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedBy = null;
            DeletedDate = null;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return CreatedBy;
            yield return CreatedDate;
            yield return ModifiedBy ?? string.Empty;
            yield return ModifiedDate ?? DateTime.MinValue;
            yield return DeletedBy ?? string.Empty;
            yield return DeletedDate ?? DateTime.MinValue;
            yield return IsDeleted;
        }
    }
}