using CourseLibrary.API.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace CourseLibrary.API.Helpers
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> ApplySort<T>(this IQueryable<T> source, string orderBy, Dictionary<string, 
            PropertyMappingValue> mappingDictionary)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if(mappingDictionary == null)
            {
                throw new ArgumentNullException(nameof(mappingDictionary));
            }

            if(string.IsNullOrWhiteSpace(orderBy))
            {
                return source;
            }

            //final orderBy
            var orderByString = string.Empty;

            // the orderBy string is separated by "," so we split it.
            var orderByAfterSplit = orderBy.Split(',');

            //apply each orderby clause in reverse order 

            foreach (var orderByClause in orderByAfterSplit.Reverse())
            {
                // trim the orderByClause for leading or trailing spaces
                var trimmedOrderByClause = orderByClause.Trim();

                //if the sort option ends with "desc" then orderby descending
                var orderDescending = trimmedOrderByClause.EndsWith(" desc");

                //remove "asc" or "desc" from the orderByClause
                var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
                var propertyName = indexOfFirstSpace == -1 ? trimmedOrderByClause : 
                    trimmedOrderByClause.Remove(indexOfFirstSpace);

                // find the matching property
                if(!mappingDictionary.ContainsKey(propertyName))
                {
                    throw new ArgumentException($"Key mapping for {propertyName} is missing");
                }

                // get the PropertyMappingValue
                var propertyMappingValue = mappingDictionary[propertyName];
                if(propertyMappingValue == null)
                {
                    throw new ArgumentException($"Value mapping for {propertyName} is missing");
                }

                // run through the property names
                foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
                {
                    //revert sord order if necessary
                    if (propertyMappingValue.Revert)
                    {
                        orderDescending = !orderDescending;
                    }

                    orderByString = orderByString + (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ",")
                        + destinationProperty
                        + (orderDescending ? " descending" : " ascending");
                }
            }

            return source.OrderBy(orderByString);
        }
    }
}
