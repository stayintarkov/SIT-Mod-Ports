using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    internal class ConfigSearchResult
    {
        public PropertyInfo PropertyInfo { get; }
        public object Object { get; }
        public object DictionaryObject { get; }

        public ConfigSearchResult(PropertyInfo _propertyInfo, object _object)
        {
            PropertyInfo = _propertyInfo;
            Object = _object;
        }

        public ConfigSearchResult(PropertyInfo _propertyInfo, object _object, object _dictObject): this(_propertyInfo, _object)
        {
            DictionaryObject = _dictObject;
        }
    }
}
