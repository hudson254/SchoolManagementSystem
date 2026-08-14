namespace SMS.Application.Features.GradingScales.DTOs
{
    public class GradingScaleDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Version { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public List<GradeBandDto> Bands { get; set; } = new();
    }

    public class GradeBandDto
    {
        public Guid Id { get; set; }
        public Guid GradingScaleId { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal MinPercentage { get; set; }
        public decimal MaxPercentage { get; set; }
        public decimal GpaPoints { get; set; }
        public string ColorCode { get; set; } = string.Empty;
        public string? HonorsClassification { get; set; }
        public int SortOrder { get; set; }
    }
}

