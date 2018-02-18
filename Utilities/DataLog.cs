using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Utilities
{
    public class DataLog
    {

    }

    public class SensorData : IEnumerable
    {
        private Dictionary<string, object> Data = new Dictionary<string, object>();

        public void Add<DataType>(string key, DataType value)// where DataType : class
        {
            this.Data.Add(key, value);
        }

        public DataType GetValue<DataType>(string key)// where DataType : class
        {
            return (DataType)this.Data[key];
        }

        public IEnumerator GetEnumerator() => this.Data.GetEnumerator();
    }
}
