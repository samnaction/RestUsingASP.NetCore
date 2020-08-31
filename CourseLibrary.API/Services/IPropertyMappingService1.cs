namespace CourseLibrary.API.Services
{
    public interface IPropertyMappingService1
    {
        bool ValidMappingExistsFor<TSource, TDestination>(string fields);
    }
}