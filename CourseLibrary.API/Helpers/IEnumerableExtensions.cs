﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;

namespace CourseLibrary.API.Helpers
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<ExpandoObject> ShapeData<TSource>(this IEnumerable<TSource> source, string fields)
        {
            if(source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            //create a list to hold the ExpandoObjects
            var expandoObjectList = new List<ExpandoObject>();

            //create a list of preoprtyInfo objects on TSource.
            //Reflection is expensive, so rather doing it for each object, we do it once and reuse the results
            var propertyInfoList = new List<PropertyInfo>();

            if (string.IsNullOrWhiteSpace(fields))
            {
                var propertyInfos = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
                propertyInfoList.AddRange(propertyInfos);
            }
            else
            {
                var fieldsAfterSplit = fields.Split(',');

                foreach (var field in fieldsAfterSplit)
                {
                    var propertyName = field.Trim();

                    var propertyInfo = typeof(TSource).GetProperty(propertyName, BindingFlags.IgnoreCase | 
                        BindingFlags.Public | BindingFlags.Instance);

                    if(propertyInfo == null)
                    {
                        throw new Exception($"Property {propertyName} wasn't found on {typeof(TSource)}");
                    }

                    // add propertyInfo to propertyInfoList
                    propertyInfoList.Add(propertyInfo);
                }

            }

            foreach (TSource sourceObject in source)
            {
                //create an ExpandoObject that will hold the selected properties and values
                var dataShapedObject = new ExpandoObject();

                foreach (var propertyInfo in propertyInfoList)
                {
                    // GetValue returns the value of the property on the source object
                    var propertyValue = propertyInfo.GetValue(sourceObject);

                    // add the field to the ExpandoObject
                    ((IDictionary<string, object>)dataShapedObject).Add(propertyInfo.Name, propertyValue);
                }

                expandoObjectList.Add(dataShapedObject);
            }

            return expandoObjectList;
        }
    }
}
