using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Formats.Alembic.Importer
{
    [Serializable]
    class SerializableDictionary<TK, TV> : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<TK, TV>>
    {
        [SerializeField]
        List<TK> keys = new();
        [SerializeField]
        List<TV> values = new();

        Dictionary<TK, TV> dict = new();


        public void ChangeOrAddKey(TK k1, TK k2)
        {
            if (dict.TryGetValue(k1, out var v))
            {
                dict.Remove(k1);
                dict.Add(k2, v);
            }
            else
            {
                dict.Add(k2, v);
            }
        }

        public void OnBeforeSerialize()
        {
            keys = dict.Keys.ToList();
            values = dict.Values.ToList();
        }

        public void OnAfterDeserialize()
        {
            dict = keys.Zip(values, (k, v) => new {k, v}).ToDictionary(x => x.k, x => x.v);
        }

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
