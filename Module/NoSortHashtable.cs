using System.Collections;
using System.Collections.Generic;

namespace MyWeb.Module
{
    public class NoSortHashtable:Hashtable
    {
        private IList keys = new List<string>();
        public NoSortHashtable()
        {
        }
        public override void Add(object key, object value)
        {
            base.Add(key, value);
            keys.Add(key);
        }
        public override ICollection Keys
        {
            get
            {
                return keys;
            }
        }
        public override void Clear()
        {
            base.Clear();
            keys.Clear();
        }
        public override void Remove(object key)
        {
            base.Remove(key);
            keys.Remove(key);
        }
        public override IDictionaryEnumerator GetEnumerator()
        {
            return base.GetEnumerator();
        }
    }
}