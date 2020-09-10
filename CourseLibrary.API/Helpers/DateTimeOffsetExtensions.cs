namespace CourseLibrary.API.Helpers
{
    using System;

    public static class DateTimeOffsetExtensions
    {
        public static int GetCurrentAge(this DateTimeOffset dateTimeOffset, DateTimeOffset? dateofDeath)
        {
            var endDate = DateTime.UtcNow;

            if(dateofDeath != null)
            {
                endDate = dateofDeath.Value.UtcDateTime;
            }

            int age = endDate.Year - dateTimeOffset.Year;

            if (endDate < dateTimeOffset.AddYears(age))
            {
                age--;
            }

            return age;
        }
    }
}
