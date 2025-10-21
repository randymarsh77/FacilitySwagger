namespace Facility.Definition.Swagger;

/// <summary>
/// Represents a Media Type Object in OpenAPI 3.0.
/// </summary>
public class OpenApiMediaType
{
	/// <summary>
	/// The schema defining the content of the request, response, or parameter.
	/// </summary>
	public SwaggerSchema? Schema { get; set; }

	/// <summary>
	/// Example of the media type.
	/// </summary>
	public object? Example { get; set; }

	/// <summary>
	/// Examples of the media type.
	/// </summary>
	public IDictionary<string, object>? Examples { get; set; }
}
