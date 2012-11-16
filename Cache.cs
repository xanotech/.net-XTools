using System;
using System.Collections.Generic;

namespace Xanotech.Tools {
    public class Cache<TKey, TValue> {

        private IDictionary<TKey, TValue> cache;



        public Cache() : this(null) {
        } // end constructor



        public Cache(Func<TKey, TValue> initializer) {
            Initializer = initializer;
        } // end constructor



        public TValue GetValue(TKey key) {
            return GetValue(key, null);
        } // end method



        public TValue GetValue(TKey key, Func<TValue> initializer) {
            TValue value;
            if (cache == null)
                cache = new Dictionary<TKey, TValue>();
            lock (cache) {
                if (cache.TryGetValue(key, out value))
                    return value;

                if (initializer == null && Initializer == null)
                    throw new NullReferenceException("Unable to create value, no initializer Func specified.");
                if (initializer != null)
                    value = initializer();
                else
                    value = Initializer(key);
                cache[key] = value;
            } // end lock
            return value;
        } // end method



        public Func<TKey, TValue> Initializer { get; set; }



        public void PutValue(TKey key, TValue value) {
            if (cache == null)
                cache = new Dictionary<TKey, TValue>();
            lock (cache)
                cache[key] = value;
        } // end method

    } // end class
} // end namespace
