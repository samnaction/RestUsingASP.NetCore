using System;
using System.Collections.Generic;

namespace CourseLibrary.API.Services
{
    public class PropertyMapping<TSource, TDestination> : IPropertyMapping
    {
        public Dictionary<string, PropertyMappingValue> _mappingDictionary { get; private set; }

        public PropertyMapping(Dictionary<string, PropertyMappingValue> mapppingDictionary)
        {
            _mappingDictionary = mapppingDictionary ?? throw new ArgumentNullException(nameof(mapppingDictionary));
        }
    }
}
