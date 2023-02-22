// Copyright 2022 ReWaffle LLC. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Naninovel
{
    public abstract class SerializableMap
    {
        protected class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>
        {
            public Dictionary () { }
            public Dictionary (IEqualityComparer<TKey> comparer) : base(comparer) { }
            public Dictionary (IDictionary<TKey, TValue> dict) : base(dict) { }
            public Dictionary (IDictionary<TKey, TValue> dict, IEqualityComparer<TKey> comparer) : base(dict, comparer) { }
            public Dictionary (SerializationInfo info, StreamingContext context) : base(info, context) { }
        }
    }

    /// <summary>
    /// Serializable version of generic C# dictionary.
    /// Derive non-generic versions to use (Unity serialization doesn't support generic types).
    /// </summary>
    /// <remarks>
    /// Implementation based on: https://github.com/azixMcAze/Unity-SerializableDictionary.
    /// </remarks>
    [Serializable]
    public abstract class SerializableMap<TKey, TValue> : SerializableMap, 
        IDictionary<TKey, TValue>, IDictionary, ISerializationCallbackReceiver, IDeserializationCallback, ISerializable
    {
        [SerializeField] private TKey[] keys;
        [SerializeField] private TValue[] values;

        private Dictionary<TKey, TValue> dictionary;

        protected SerializableMap ()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        protected SerializableMap (IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        protected SerializableMap (IDictionary<TKey, TValue> dict)
        {
            dictionary = new Dictionary<TKey, TValue>(dict);
        }

        protected SerializableMap (IDictionary<TKey, TValue> dict, IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(dict, comparer);
        }

        #region ISerializationCallbackReceiver

        public void OnAfterDeserialize ()
        {
            if (keys != null && values != null && keys.Length == values.Length)
            {
                dictionary.Clear();
                for (int i = 0; i < keys.Length; ++i)
                    dictionary[keys[i]] = GetValue(values, i);

                keys = null;
                values = null;
            }
        }

        public void OnBeforeSerialize ()
        {
            var length = dictionary.Count;
            keys = new TKey[length];
            values = new TValue[length];

            var i = 0;
            foreach (var kvp in dictionary)
            {
                keys[i] = kvp.Key;
                SetValue(values, i, kvp.Value);
                ++i;
            }
        }

        #endregion

        #region IDictionary<TKey, TValue>

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>)dictionary).Keys;
        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>)dictionary).Values;
        public int Count => ((IDictionary<TKey, TValue>)dictionary).Count;
        public bool IsReadOnly => ((IDictionary<TKey, TValue>)dictionary).IsReadOnly;

        public TValue this[TKey key]
        {
            get => ((IDictionary<TKey, TValue>)dictionary)[key];
            set => ((IDictionary<TKey, TValue>)dictionary)[key] = value;
        }

        public void Add (TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>)dictionary).Add(key, value);
        }

        public bool ContainsKey (TKey key)
        {
            return ((IDictionary<TKey, TValue>)dictionary).ContainsKey(key);
        }

        public bool Remove (TKey key)
        {
            return ((IDictionary<TKey, TValue>)dictionary).Remove(key);
        }

        public bool TryGetValue (TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>)dictionary).TryGetValue(key, out value);
        }

        public void Add (KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>)dictionary).Add(item);
        }

        public void Clear ()
        {
            ((IDictionary<TKey, TValue>)dictionary).Clear();
        }

        public bool Contains (KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)dictionary).Contains(item);
        }

        public void CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>)dictionary).CopyTo(array, arrayIndex);
        }

        public bool Remove (KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>)dictionary).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
        {
            return ((IDictionary<TKey, TValue>)dictionary).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return ((IDictionary<TKey, TValue>)dictionary).GetEnumerator();
        }

        #endregion

        #region IDictionary

        public bool IsFixedSize => ((IDictionary)dictionary).IsFixedSize;
        ICollection IDictionary.Keys => ((IDictionary)dictionary).Keys;
        ICollection IDictionary.Values => ((IDictionary)dictionary).Values;
        public bool IsSynchronized => ((IDictionary)dictionary).IsSynchronized;
        public object SyncRoot => ((IDictionary)dictionary).SyncRoot;

        public object this[object key]
        {
            get => ((IDictionary)dictionary)[key];
            set => ((IDictionary)dictionary)[key] = value;
        }

        public void Add (object key, object value)
        {
            ((IDictionary)dictionary).Add(key, value);
        }

        public bool Contains (object key)
        {
            return ((IDictionary)dictionary).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator ()
        {
            return ((IDictionary)dictionary).GetEnumerator();
        }

        public void Remove (object key)
        {
            ((IDictionary)dictionary).Remove(key);
        }

        public void CopyTo (Array array, int index)
        {
            ((IDictionary)dictionary).CopyTo(array, index);
        }

        #endregion

        #region IDeserializationCallback

        public void OnDeserialization (object sender)
        {
            ((IDeserializationCallback)dictionary).OnDeserialization(sender);
        }

        #endregion

        #region ISerializable

        protected SerializableMap (SerializationInfo info, StreamingContext context)
        {
            dictionary = new Dictionary<TKey, TValue>(info, context);
        }

        public void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            ((ISerializable)dictionary).GetObjectData(info, context);
        }

        #endregion

        protected virtual TValue GetValue (TValue[] storage, int i)
        {
            return storage[i];
        }

        protected virtual void SetValue (TValue[] storage, int i, TValue value)
        {
            storage[i] = value;
        }
    }
}
