using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gosuman.EntitySystem.Serialization
{
    public class SerializerTypeContext
    {
        private Dictionary<string, Type> typeDictionary = new Dictionary<string, Type>();

        public SerializerTypeContext(IEnumerable<Assembly?> assemblies) : this()
        {
            foreach (var assembly in assemblies)
            {
                if (assembly != null)
                {
                    LoadTypesFromAssembly(assembly);
                }
            }
        }

        public SerializerTypeContext()
        {
            var entityAss = Assembly.GetAssembly(typeof(Entity));
            LoadTypesFromAssembly(entityAss!);
            if (Assembly.GetCallingAssembly() != entityAss)
            {
                LoadTypesFromAssembly(Assembly.GetCallingAssembly());
            }
        }

        private void LoadTypesFromAssembly(Assembly a)
        {
            if (a == null)
            {
                return;
            }
            /// Get all available component types for parsing
            foreach (var tp in a.GetTypes())
            {
                if (tp.IsSubclassOf(typeof(Entity)) || typeof(IComponent).IsAssignableFrom(tp))
                {
                    typeDictionary[tp.Name.ToLowerInvariant()] = tp;
                }
            }
        }

        public string SanitizeComponentName(Type type)
        {
            var name = type.Name.ToLowerInvariant();
            if (name.EndsWith("component"))
            {
                return name.Substring(0, name.Length - 9);
            }
            return name;
        }

        public string DesanitizeComponentName(string name)
        {
            if (typeDictionary.ContainsKey(name))
            {
                return name;
            }
            return name + "component";
        }

        public bool HasType(string typeName) => typeDictionary.ContainsKey(typeName);

        public Type GetType(string typeName) => typeDictionary[typeName];
    }
}
