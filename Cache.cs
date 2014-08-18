using System;
using System.Collections.Concurrent;

namespace XTools {
    public class Cache<TKey, TValue> {

        private ConcurrentDictionary<TKey, TValue> cache = new ConcurrentDictionary<TKey, TValue>();



        public Cache() : this(null) {
        } // end constructor



        public Cache(Func<TKey, TValue> initializer) {
            Initializer = initializer;
        } // end constructor



        public TValue GetValue(TKey key) {
            return GetValue(key, (Func<TValue>)null);
        } // end method



        public TValue GetValue(TKey key, Func<TValue> initializer) {
            TValue value;
            if (initializer != null)
                value = cache.GetOrAdd(key, k => initializer());
            else
                value = cache.GetOrAdd(key, Initializer);
            return value;
        } // end method



        public TValue GetValue(TKey key, Func<TKey, TValue> initializer) {
            TValue value;
            if (initializer != null)
                value = cache.GetOrAdd(key, initializer);
            else
                value = cache.GetOrAdd(key, Initializer);
            return value;
        } // end method



        public Func<TKey, TValue> Initializer { get; set; }



        public void PutValue(TKey key, TValue value) {
            cache[key] = value;
        } // end method



        public TValue this[TKey key] {
            get {
                return GetValue(key);
            } set {
                PutValue(key, value);
            } // end get-set
        } // end indexer

    } // end class
} // end namespace
