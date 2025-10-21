using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace Facility.Definition.Swagger;

/// <summary>
/// Represents an OpenAPI 3.0 document.
/// </summary>
public class OpenApiDocument
{
	/// <summary>
	/// The OpenAPI version (e.g., "3.0.0").
	/// </summary>
	[JsonProperty("openapi")]
	[YamlMember(Alias = "openapi")]
	public string? OpenApi { get; set; }

	/// <summary>
	/// Provides metadata about the API.
	/// </summary>
	public SwaggerInfo? Info { get; set; }

	/// <summary>
	/// An array of Server Objects, which provide connectivity information to a target server.
	/// </summary>
	public IList<OpenApiServer>? Servers { get; set; }

	/// <summary>
	/// The available paths and operations for the API.
	/// </summary>
	public IDictionary<string, SwaggerOperations>? Paths { get; set; }

	/// <summary>
	/// An element to hold various schemas for the specification.
	/// </summary>
	public OpenApiComponents? Components { get; set; }

	/// <summary>
	/// A declaration of which security mechanisms can be used across the API.
	/// </summary>
	public IList<IDictionary<string, IList<string>>>? Security { get; set; }

	/// <summary>
	/// A list of tags used by the specification with additional metadata.
	/// </summary>
	public IList<SwaggerTag>? Tags { get; set; }

	/// <summary>
	/// Additional external documentation.
	/// </summary>
	public SwaggerExternalDocumentation? ExternalDocs { get; set; }
}
