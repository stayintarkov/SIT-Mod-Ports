using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTTPConfigEditor.Configuration
{
    internal class ConfigDictionaryEntry<TKey, TValue>
    {
        public Type TypeKey
        {
            get { return typeof(TKey); }
        }

        public Type TypeValue
        {
            get { return typeof(TValue); }
        }

        public ConfigDictionaryEntry()
        {

        }
    }
}
