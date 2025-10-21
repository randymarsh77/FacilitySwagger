using Newtonsoft.Json.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Facility.Definition.Swagger;

/// <summary>
/// Custom YAML type converter for Newtonsoft.Json JToken types.
/// Converts YAML nodes (scalars, sequences, mappings) into JToken objects.
/// </summary>
internal sealed class JTokenYamlConverter : IYamlTypeConverter
{
	/// <summary>
	/// Accepts JToken and derived types (JArray, JObject, JValue).
	/// </summary>
	public bool Accepts(Type type)
	{
		return type == typeof(JToken) ||
			type == typeof(JArray) ||
			type == typeof(JObject) ||
			type == typeof(JValue) ||
			type == typeof(IList<JToken>);
	}

	/// <summary>
	/// Reads YAML and converts to JToken.
	/// </summary>
	public object? ReadYaml(IParser parser, Type type)
	{
		if (parser.Current == null)
			return null;

		// Handle different YAML node types
		return parser.Current switch
		{
			Scalar scalar => ReadScalar(parser, scalar),
			SequenceStart => ReadSequence(parser),
			MappingStart => ReadMapping(parser),
			_ => throw new YamlException($"Unexpected YAML node type: {parser.Current.GetType().Name}"),
		};
	}

	/// <summary>
	/// Writes JToken to YAML (not implemented - uses default serialization).
	/// </summary>
	public void WriteYaml(IEmitter emitter, object? value, Type type)
	{
		// Not needed for deserialization-only scenario
		throw new NotImplementedException("JToken serialization to YAML is not supported.");
	}

	private static JValue ReadScalar(IParser parser, Scalar scalar)
	{
		parser.MoveNext();

		// Try to infer the type from the scalar value
		var value = scalar.Value;

		// Handle null/empty
		if (string.IsNullOrEmpty(value) || value == "~" || value == "null")
			return JValue.CreateNull();

		// Handle boolean
		if (value == "true" || value == "false")
			return new JValue(bool.Parse(value));

		// Handle numbers
		if (long.TryParse(value, out var longValue))
			return new JValue(longValue);

		if (double.TryParse(value, out var doubleValue))
			return new JValue(doubleValue);

		// Default to string
		return new JValue(value);
	}

	private static JArray ReadSequence(IParser parser)
	{
		var array = new JArray();
		parser.MoveNext(); // Move past SequenceStart

		while (parser.Current is not SequenceEnd)
		{
			var item = ReadValue(parser);
			if (item != null)
				array.Add(item);
		}

		parser.MoveNext(); // Move past SequenceEnd
		return array;
	}

	private static JObject ReadMapping(IParser parser)
	{
		var obj = new JObject();
		parser.MoveNext(); // Move past MappingStart

		while (parser.Current is not MappingEnd)
		{
			// Read key
			if (parser.Current is not Scalar keyScalar)
				throw new YamlException("Expected scalar key in mapping");

			var key = keyScalar.Value;
			parser.MoveNext();

			// Read value
			var value = ReadValue(parser);
			if (value != null)
				obj[key] = value;
		}

		parser.MoveNext(); // Move past MappingEnd
		return obj;
	}

	private static JToken? ReadValue(IParser parser)
	{
		if (parser.Current == null)
			return null;

		return parser.Current switch
		{
			Scalar scalar => ReadScalar(parser, scalar),
			SequenceStart => ReadSequence(parser),
			MappingStart => ReadMapping(parser),
			_ => throw new YamlException($"Unexpected YAML node type: {parser.Current.GetType().Name}"),
		};
	}
}
