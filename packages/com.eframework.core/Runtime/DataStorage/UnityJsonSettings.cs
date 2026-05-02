using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace EFrame.Runtime.DataStorage
{
    /// <summary>
    /// Shared Json.NET settings tuned for Unity projects to avoid self-referencing loops
    /// and to correctly serialize common Unity types.
    /// </summary>
    public static class UnityJsonSettings
    {
        // Naming convention: static readonly fields prefixed with s_
        public static readonly JsonSerializerSettings s_settings = CreateSettings();

        // Keep a friendly accessor matching previous usage
        public static JsonSerializerSettings Settings => s_settings;

        private static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                // Prevent errors like: Self referencing loop detected for property 'normalized'
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // Be lenient when fields are removed/renamed across versions
                MissingMemberHandling = MissingMemberHandling.Ignore,
                // Skip nulls for cleaner output
                NullValueHandling = NullValueHandling.Ignore,
                // Ignore read-only properties (like Vector3.normalized, magnitude, etc.)
                ContractResolver = new WritablePropertiesOnlyResolver()
            };

            // Converters for common Unity structs
            settings.Converters.Add(new Vector2Converter());
            settings.Converters.Add(new Vector3Converter());
            settings.Converters.Add(new QuaternionConverter());
            settings.Converters.Add(new ColorConverter());

            return settings;
        }
    }

    /// <summary>
    /// Contract resolver that ignores properties without a public setter.
    /// This avoids serializing Unity computed/read-only properties that can cause loops.
    /// </summary>
    internal class WritablePropertiesOnlyResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (!property.Writable)
            {
                // Do not serialize read-only properties
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }

    internal class Vector2Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Vector2);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = (Vector2)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(v.x);
            writer.WritePropertyName("y"); writer.WriteValue(v.y);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (reader.TokenType != JsonToken.StartObject)
            {
                // Try to handle array form [x,y]
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read(); x = Convert.ToSingle(reader.Value);
                    reader.Read(); y = Convert.ToSingle(reader.Value);
                    // consume EndArray
                    while (reader.TokenType != JsonToken.EndArray && reader.Read()) { }
                    return new Vector2(x, y);
                }
                return Vector2.zero;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var name = (string)reader.Value;
                if (!reader.Read()) break;
                switch (name)
                {
                    case "x": x = Convert.ToSingle(reader.Value); break;
                    case "y": y = Convert.ToSingle(reader.Value); break;
                }
            }
            return new Vector2(x, y);
        }
    }

    internal class Vector3Converter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Vector3);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var v = (Vector3)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(v.x);
            writer.WritePropertyName("y"); writer.WriteValue(v.y);
            writer.WritePropertyName("z"); writer.WriteValue(v.z);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Vector3.zero;
            float x = 0f, y = 0f, z = 0f;
            if (reader.TokenType != JsonToken.StartObject)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read(); x = Convert.ToSingle(reader.Value);
                    reader.Read(); y = Convert.ToSingle(reader.Value);
                    reader.Read(); z = Convert.ToSingle(reader.Value);
                    while (reader.TokenType != JsonToken.EndArray && reader.Read()) { }
                    return new Vector3(x, y, z);
                }
                return Vector3.zero;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var name = (string)reader.Value;
                if (!reader.Read()) break;
                switch (name)
                {
                    case "x": x = Convert.ToSingle(reader.Value); break;
                    case "y": y = Convert.ToSingle(reader.Value); break;
                    case "z": z = Convert.ToSingle(reader.Value); break;
                }
            }
            return new Vector3(x, y, z);
        }
    }

    internal class QuaternionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Quaternion);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var q = (Quaternion)value;
            writer.WriteStartObject();
            writer.WritePropertyName("x"); writer.WriteValue(q.x);
            writer.WritePropertyName("y"); writer.WriteValue(q.y);
            writer.WritePropertyName("z"); writer.WriteValue(q.z);
            writer.WritePropertyName("w"); writer.WriteValue(q.w);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Quaternion.identity;
            float x = 0f, y = 0f, z = 0f, w = 1f;
            if (reader.TokenType != JsonToken.StartObject)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read(); x = Convert.ToSingle(reader.Value);
                    reader.Read(); y = Convert.ToSingle(reader.Value);
                    reader.Read(); z = Convert.ToSingle(reader.Value);
                    reader.Read(); w = Convert.ToSingle(reader.Value);
                    while (reader.TokenType != JsonToken.EndArray && reader.Read()) { }
                    return new Quaternion(x, y, z, w);
                }
                return Quaternion.identity;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var name = (string)reader.Value;
                if (!reader.Read()) break;
                switch (name)
                {
                    case "x": x = Convert.ToSingle(reader.Value); break;
                    case "y": y = Convert.ToSingle(reader.Value); break;
                    case "z": z = Convert.ToSingle(reader.Value); break;
                    case "w": w = Convert.ToSingle(reader.Value); break;
                }
            }
            return new Quaternion(x, y, z, w);
        }
    }

    internal class ColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Color);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var c = (Color)value;
            writer.WriteStartObject();
            writer.WritePropertyName("r"); writer.WriteValue(c.r);
            writer.WritePropertyName("g"); writer.WriteValue(c.g);
            writer.WritePropertyName("b"); writer.WriteValue(c.b);
            writer.WritePropertyName("a"); writer.WriteValue(c.a);
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Color.white;
            float r = 1f, g = 1f, b = 1f, a = 1f;
            if (reader.TokenType != JsonToken.StartObject)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read(); r = Convert.ToSingle(reader.Value);
                    reader.Read(); g = Convert.ToSingle(reader.Value);
                    reader.Read(); b = Convert.ToSingle(reader.Value);
                    reader.Read(); a = Convert.ToSingle(reader.Value);
                    while (reader.TokenType != JsonToken.EndArray && reader.Read()) { }
                    return new Color(r, g, b, a);
                }
                return Color.white;
            }
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndObject) break;
                if (reader.TokenType != JsonToken.PropertyName) continue;
                var name = (string)reader.Value;
                if (!reader.Read()) break;
                switch (name)
                {
                    case "r": r = Convert.ToSingle(reader.Value); break;
                    case "g": g = Convert.ToSingle(reader.Value); break;
                    case "b": b = Convert.ToSingle(reader.Value); break;
                    case "a": a = Convert.ToSingle(reader.Value); break;
                }
            }
            return new Color(r, g, b, a);
        }
    }
}
