using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xanotech.Tools {
    public class Mirror {

        private Cache<string, MethodInfo> cacheGetMethod_String_Type0;
        private Cache<Tuple<string, Type>, MethodInfo> cacheGetMethod_String_Type1;
        private Cache<Tuple<string, Type, Type>, MethodInfo> cacheGetMethod_String_Type2;
        private Cache<Tuple<string, Type, Type, Type>, MethodInfo> cacheGetMethod_String_Type3;
        private Cache<Tuple<string, Type, Type, Type, Type>, MethodInfo> cacheGetMethod_String_Type4;
        private Cache<string, PropertyInfo> cacheGetProperty_String;
        private Cache<Tuple<string, BindingFlags>, PropertyInfo> cacheGetProperty_String_BindingFlags;
        private Cache<Type, bool> cacheIsAssignableFrom;
        private Cache<Type, Type> cacheMakeGenericType1;
        private Cache<Tuple<Type, Type>, Type> cacheMakeGenericType2;
        private Cache<Tuple<Type, Type, Type>, Type> cacheMakeGenericType3;
        private Cache<Tuple<Type, Type, Type, Type>, Type> cacheMakeGenericType4;
        private PropertyInfo[] properties;
        private Type reflectedType;



        public Mirror(Type type) {
            cacheGetMethod_String_Type0 = new Cache<string, MethodInfo>();
            cacheGetMethod_String_Type1 = new Cache<Tuple<string, Type>, MethodInfo>();
            cacheGetMethod_String_Type2 = new Cache<Tuple<string, Type, Type>, MethodInfo>();
            cacheGetMethod_String_Type3 = new Cache<Tuple<string, Type, Type, Type>, MethodInfo>();
            cacheGetMethod_String_Type4 = new Cache<Tuple<string, Type, Type, Type, Type>, MethodInfo>();
            cacheGetProperty_String = new Cache<string, PropertyInfo>();
            cacheGetProperty_String_BindingFlags = new Cache<Tuple<string, BindingFlags>, PropertyInfo>();            
            cacheIsAssignableFrom = new Cache<Type, bool>();
            cacheMakeGenericType1 = new Cache<Type, Type>();
            cacheMakeGenericType2 = new Cache<Tuple<Type,Type>,Type>();
            cacheMakeGenericType3 = new Cache<Tuple<Type,Type,Type>,Type>();
            cacheMakeGenericType4 = new Cache<Tuple<Type,Type,Type,Type>,Type>();
            reflectedType = type;
        } // end constructor



        public MethodInfo GetMethod(string name, Type[] types) {
            MethodInfo method;
            if (types == null || types.Length == 0)
                method = cacheGetMethod_String_Type0.GetValue(name, () => reflectedType.GetMethod(name, types));
            else if (types.Length == 1) {
                var key = new Tuple<string, Type>(name, types[0]);
                method = cacheGetMethod_String_Type1.GetValue(key, () => reflectedType.GetMethod(name, types));
            } else if (types.Length == 2) {
                var key = new Tuple<string, Type, Type>(name, types[0], types[1]);
                method = cacheGetMethod_String_Type2.GetValue(key, () => reflectedType.GetMethod(name, types));
            } else if (types.Length == 3) {
                var key = new Tuple<string, Type, Type, Type>(name, types[0], types[1], types[2]);
                method = cacheGetMethod_String_Type3.GetValue(key, () => reflectedType.GetMethod(name, types));
            } else if (types.Length == 4) {
                var key = new Tuple<string, Type, Type, Type, Type>(name, types[0], types[1], types[2], types[3]);
                method = cacheGetMethod_String_Type4.GetValue(key, () => reflectedType.GetMethod(name, types));
            } else
                method = reflectedType.GetMethod(name, types);
            return method;
        } // end method



        public PropertyInfo[] GetProperties() {
            return properties ?? (properties = reflectedType.GetProperties());
        } // end method



        public PropertyInfo GetProperty(string name) {
            //return cacheGetProperty_String.GetValue(name, () => reflectedType.GetProperty(name));
            return reflectedType.GetProperty(name);
        } // end method



        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) {
            //var key = new Tuple<string, BindingFlags>(name, bindingAttr);
            //return cacheGetProperty_String_BindingFlags.GetValue(key, () => reflectedType.GetProperty(key.Item1, key.Item2));
            return reflectedType.GetProperty(name, bindingAttr);
        } // end method



        public bool IsAssignableFrom(Type c) {
            return cacheIsAssignableFrom.GetValue(c, () => reflectedType.IsAssignableFrom(c));
        } // end method



        public Type MakeGenericType(params Type[] typeArguments) {
            Type type;
            if (typeArguments == null || typeArguments.Length == 0)
                type = reflectedType.MakeGenericType(typeArguments);
            else if (typeArguments.Length == 1)
                type = cacheMakeGenericType1.GetValue(typeArguments[0], () => reflectedType.MakeGenericType(typeArguments));
            else if (typeArguments.Length == 2) {
                var key = new Tuple<Type, Type>(typeArguments[0], typeArguments[1]);
                type = cacheMakeGenericType2.GetValue(key, () => reflectedType.MakeGenericType(typeArguments));
            } else if (typeArguments.Length == 3) {
                var key = new Tuple<Type, Type, Type>(typeArguments[0], typeArguments[1], typeArguments[2]);
                type = cacheMakeGenericType3.GetValue(key, () => reflectedType.MakeGenericType(typeArguments));
            } else if (typeArguments.Length == 4) {
                var key = new Tuple<Type, Type, Type, Type>(typeArguments[0], typeArguments[1], typeArguments[2], typeArguments[4]);
                type = cacheMakeGenericType4.GetValue(key, () => reflectedType.MakeGenericType(typeArguments));
            } else
                type = reflectedType.MakeGenericType(typeArguments);
            return type;
        } // end method



        public Type ReflectedType {
            get { return reflectedType; }
        } // end property

    } // end class
} // end namespace
