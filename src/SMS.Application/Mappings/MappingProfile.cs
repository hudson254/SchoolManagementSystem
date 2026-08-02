using AutoMapper;
using SMS.Application.DTOs;
using SMS.Domain.Entities;

namespace SMS.Application.Mappings
{
    /// <summary>
    /// AutoMapper configuration profile
    /// </summary>
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Student mappings
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.FullName, 
                    opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.FirstName, 
                    opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, 
                    opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Email, 
                    opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, 
                    opt => opt.MapFrom(src => src.User.PhoneNumber))
                .ForMember(dest => dest.Organization, 
                    opt => opt.MapFrom(src => src.User.Organization))
                .ForMember(dest => dest.ProgrammeName, 
                    opt => opt.MapFrom(src => src.Programme != null ? src.Programme.Name : null));

            CreateMap<Student, StudentDetailsDto>()
                .IncludeBase<Student, StudentDto>()
                .ForMember(dest => dest.CurrentSemesterName, 
                    opt => opt.MapFrom(src => src.CurrentSemester != null ? src.CurrentSemester.Name : null))
                .ForMember(dest => dest.CurrentSemesterNumber, 
                    opt => opt.MapFrom(src => src.CurrentSemester != null ? src.CurrentSemester.SemesterNumber : 0))
                .ForMember(dest => dest.TotalEnrollments, 
                    opt => opt.MapFrom(src => src.Enrollments.Count))
                .ForMember(dest => dest.CompletedUnits, 
                    opt => opt.MapFrom(src => src.Enrollments.Count(e => e.Status == "Completed")))
                .ForMember(dest => dest.InProgressUnits, 
                    opt => opt.MapFrom(src => src.Enrollments.Count(e => e.Status == "InProgress")));

            // Lecturer mappings
            CreateMap<Lecturer, LecturerDto>()
                .ForMember(dest => dest.FullName, 
                    opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.FirstName, 
                    opt => opt.MapFrom(src => src.User.FirstName))
                .ForMember(dest => dest.LastName, 
                    opt => opt.MapFrom(src => src.User.LastName))
                .ForMember(dest => dest.Email, 
                    opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.PhoneNumber, 
                    opt => opt.MapFrom(src => src.User.PhoneNumber));

            // Course mappings
            CreateMap<Course, CourseDto>()
                .ForMember(dest => dest.DepartmentName, 
                    opt => opt.MapFrom(src => src.Department.Name));

            // Unit mappings
            CreateMap<Unit, UnitDto>()
                .ForMember(dest => dest.CourseName, 
                    opt => opt.MapFrom(src => src.Course.Name))
                .ForMember(dest => dest.PrerequisiteCode, 
                    opt => opt.MapFrom(src => src.Prerequisite != null ? src.Prerequisite.Code : null))
                .ForMember(dest => dest.PrerequisiteName, 
                    opt => opt.MapFrom(src => src.Prerequisite != null ? src.Prerequisite.Name : null));

            // Enrollment mappings
            CreateMap<StudentEnrollment, EnrollmentSummaryDto>()
                .ForMember(dest => dest.UnitName, 
                    opt => opt.MapFrom(src => src.Unit.Name))
                .ForMember(dest => dest.UnitCode, 
                    opt => opt.MapFrom(src => src.Unit.Code))
                .ForMember(dest => dest.Credits, 
                    opt => opt.MapFrom(src => src.Unit.Credits))
                .ForMember(dest => dest.SemesterName, 
                    opt => opt.MapFrom(src => src.Semester.Name));

            // Grade mappings
            CreateMap<Grade, GradeSummaryDto>()
                .ForMember(dest => dest.UnitName, 
                    opt => opt.MapFrom(src => src.Enrollment.Unit.Name))
                .ForMember(dest => dest.UnitCode, 
                    opt => opt.MapFrom(src => src.Enrollment.Unit.Code))
                .ForMember(dest => dest.Credits, 
                    opt => opt.MapFrom(src => src.Enrollment.Unit.Credits))
                .ForMember(dest => dest.SemesterName, 
                    opt => opt.MapFrom(src => src.Enrollment.Semester.Name));

            // Accommodation mappings
            CreateMap<AccommodationAssignment, AccommodationAssignmentDto>()
                .ForMember(dest => dest.StudentName, 
                    opt => opt.MapFrom(src => src.Student.User.FullName))
                .ForMember(dest => dest.StudentNumber, 
                    opt => opt.MapFrom(src => src.Student.StudentNumber))
                .ForMember(dest => dest.RoomNumber, 
                    opt => opt.MapFrom(src => src.Room.RoomNumber))
                .ForMember(dest => dest.BlockName, 
                    opt => opt.MapFrom(src => src.Room.Block.Name))
                .ForMember(dest => dest.BuildingName, 
                    opt => opt.MapFrom(src => src.Room.Block.Building.Name))
                .ForMember(dest => dest.SemesterName, 
                    opt => opt.MapFrom(src => src.Semester.Name));

            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Roles, 
                    opt => opt.Ignore());
        }
    }
}