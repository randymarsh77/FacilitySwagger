using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Facility.Definition.Swagger;

/// <summary>
/// Helpers for OpenAPI 3.0.
/// </summary>
public static class OpenApiUtility
{
	/// <summary>
	/// The OpenAPI version.
	/// </summary>
	public static readonly string OpenApiVersion = "3.0.0";

	/// <summary>
	/// JSON serializer settings for OpenAPI DTOs.
	/// </summary>
	public static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
	{
		ContractResolver = new CamelCaseExceptDictionaryKeysContractResolver(),
		DateParseHandling = DateParseHandling.None,
		NullValueHandling = NullValueHandling.Ignore,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
	};

	private sealed class CamelCaseExceptDictionaryKeysContractResolver : CamelCasePropertyNamesContractResolver
	{
		protected override string ResolveDictionaryKey(string dictionaryKey) => dictionaryKey;
	}
}
