namespace Facility.Definition.Swagger;

/// <summary>
/// Represents a Server Object in OpenAPI 3.0.
/// </summary>
public class OpenApiServer
{
	/// <summary>
	/// A URL to the target host.
	/// </summary>
	public string? Url { get; set; }

	/// <summary>
	/// An optional string describing the host designated by the URL.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// A map between a variable name and its value for server URL template substitution.
	/// </summary>
	public IDictionary<string, OpenApiServerVariable>? Variables { get; set; }
}
