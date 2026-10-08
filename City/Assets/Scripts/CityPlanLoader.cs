// CityPlanLoader.cs
// Loads and deserializes a city_plan.json into a CityPlan C# object.
// Uses Newtonsoft.Json with a custom converter for Position ([x, z] arrays).

using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class CityPlanLoader
{
    /// <summary>
    /// Load and deserialize a CityPlan from a JSON file path.
    /// Throws on file-not-found or parse errors so callers know immediately.
    /// </summary>
    public static CityPlan Load(string path)
    {
        Debug.Log("[CityPlan] Loading city plan from: " + path);

        if (!File.Exists(path))
            throw new FileNotFoundException(
                $"[CityPlan] City plan JSON not found at path: {path}", path);

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            throw new IOException(
                $"[CityPlan] Failed to read city plan file: {ex.Message}", ex);
        }

        return LoadFromJson(json);
    }

    /// <summary>
    /// Load and deserialize a CityPlan from a Unity TextAsset.
    /// </summary>
    public static CityPlan LoadFromTextAsset(TextAsset asset)
    {
        if (asset == null)
            throw new ArgumentNullException(nameof(asset),
                "[CityPlan] TextAsset is null. Assign the city plan JSON in the Inspector.");

        Debug.Log("[CityPlan] Loading city plan from TextAsset: " + asset.name);
        return LoadFromJson(asset.text);
    }

    /// <summary>
    /// Deserialize a CityPlan from a raw JSON string.
    /// </summary>
    public static CityPlan LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException(
                "[CityPlan] JSON string is empty or null.", nameof(json));

        JsonSerializerSettings settings = BuildSettings();

        CityPlan plan;
        try
        {
            plan = JsonConvert.DeserializeObject<CityPlan>(json, settings);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"[CityPlan] JSON parse failed: {ex.Message}\n{ex.StackTrace}", ex);
        }

        if (plan == null)
            throw new InvalidOperationException(
                "[CityPlan] Deserialization returned null. JSON may be empty or invalid.");

        Debug.Log("[CityPlan] JSON parsed successfully.");
        return plan;
    }

    // ─── Settings ────────────────────────────────────────────────────────────

    private static JsonSerializerSettings BuildSettings()
    {
        return new JsonSerializerSettings
        {
            // Converts [x, z] arrays in JSON to Position objects
            Converters = { new PositionConverter() },
            // Allow missing members in the JSON (schema evolves over time)
            MissingMemberHandling = MissingMemberHandling.Ignore,
            // Null values are allowed
            NullValueHandling = NullValueHandling.Ignore,
            // Use default values from C# classes when a field is absent
            DefaultValueHandling = DefaultValueHandling.Populate,
        };
    }

    // ─── Position Converter ───────────────────────────────────────────────────
    // Schema: position is a [x, z] two-element number array.
    // C#:     Position class with float x, float z.

    private class PositionConverter : JsonConverter<Position>
    {
        public override Position ReadJson(
            JsonReader reader,
            Type objectType,
            Position existingValue,
            bool hasExistingValue,
            JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            // Array form: [x, z]
            if (token.Type == JTokenType.Array)
            {
                JArray arr = (JArray)token;
                return new Position
                {
                    x = arr.Count > 0 ? arr[0].Value<float>() : 0f,
                    z = arr.Count > 1 ? arr[1].Value<float>() : 0f
                };
            }

            // Object form fallback: { "x": ..., "z": ... }
            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;
                return new Position
                {
                    x = obj["x"] != null ? obj["x"].Value<float>() : 0f,
                    z = obj["z"] != null ? obj["z"].Value<float>() : 0f
                };
            }

            Debug.LogWarning("[CityPlan] Unexpected Position token type: " + token.Type);
            return new Position { x = 0f, z = 0f };
        }

        public override void WriteJson(
            JsonWriter writer,
            Position value,
            JsonSerializer serializer)
        {
            writer.WriteStartArray();
            writer.WriteValue(value.x);
            writer.WriteValue(value.z);
            writer.WriteEndArray();
        }
    }
}
