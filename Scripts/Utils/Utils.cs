using Godot;
using System.Collections.Generic;
using System;
using System.Linq;

namespace GodotModules
{
    public static class Utils
    {
        public static string StringifyDict<Key, Value>(Dictionary<Key, Value> dict) => string.Join(" ", dict.Select(x => $"{x.Key} {x.Value}"));

        public static float Remap(this float value, float from1, float to1, float from2, float to2)
        {
            return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
        }

        public static Vector2 Lerp(Vector2 a, Vector2 b, float by)
        {
            float retX = Mathf.Lerp(a.x, b.x, by);
            float retY = Mathf.Lerp(a.y, b.y, by);
            return new Vector2(retX, retY);
        }

        public static Dictionary<Key, Value> LoadInstances<Key, Value, Namespace>() =>
            typeof(Value).Assembly.GetTypes()
                .Where(x => typeof(Value).IsAssignableFrom(x) && !x.IsAbstract && x.Namespace == typeof(Namespace).Namespace)
                .Select(Activator.CreateInstance).Cast<Value>()
                .ToDictionary(x => (Key)Enum.Parse(typeof(Key), x.GetType().Name.Replace(typeof(Value).Name, "")), x => x);
    }
}