namespace SMS.Application.DTOs
{
    public class ProgrammeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public int Duration { get; set; }
        public bool IsActive { get; set; }
    }
}