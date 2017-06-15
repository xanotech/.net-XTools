using System;
using System.Collections.Concurrent;

namespace XTools {
    public class Cache<TKey, TValue> {

        private ConcurrentDictionary<TKey, Lazy<TValue>> cache = new ConcurrentDictionary<TKey, Lazy<TValue>>();



        public Cache() : this(null) {
        } // end constructor



        public Cache(Func<TKey, TValue> initializer) {
            Initializer = initializer;
        } // end constructor



        public void Clear() {
            cache.Clear();
        } // end method



        public TValue GetValue(TKey key) {
            return GetValue(key, (Func<TValue>)null);
        } // end method



        public TValue GetValue(TKey key, Func<TValue> initializer) {
            TValue value;
            if (initializer != null)
                value = cache.GetOrAdd(key,
                    k => new Lazy<TValue>(initializer)).Value;
            else
                value = cache.GetOrAdd(key,
                    k => new Lazy<TValue>(() => Initializer(k))).Value;
            return value;
        } // end method



        public TValue GetValue(TKey key, Func<TKey, TValue> initializer) {
            TValue value;
            if (initializer != null)
                value = cache.GetOrAdd(key,
                    k => new Lazy<TValue>(() => initializer(k))).Value;
            else
                value = cache.GetOrAdd(key,
                    k => new Lazy<TValue>(() => Initializer(k))).Value;
            return value;
        } // end method



        public Func<TKey, TValue> Initializer { get; set; }



        public void PutValue(TKey key, TValue value) {
            cache[key] = new Lazy<TValue>(() => value);
        } // end method



        public void RemoveValue(TKey key) {
            cache.TryRemove(key);
        } // end method



        public TValue this[TKey key] {
            get {
                return GetValue(key);
            } // end get
            set {
                PutValue(key, value);
            } // end set
        } // end indexer

    } // end class
} // end namespace
