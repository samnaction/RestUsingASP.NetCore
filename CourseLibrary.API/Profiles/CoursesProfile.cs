namespace CourseLibrary.API.Profiles
{
    using AutoMapper;

    public class CoursesProfile : Profile
    {
        public CoursesProfile()
        {
            CreateMap<Entities.Course, Models.CourseDto>();
            CreateMap<Models.CourseForCreationDto, Entities.Course>();
            CreateMap<Models.CourseForUpdationDto, Entities.Course>();
            CreateMap<Entities.Course, Models.CourseForUpdationDto>();
        }
    }
}
