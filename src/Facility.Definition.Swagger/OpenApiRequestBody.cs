namespace Facility.Definition.Swagger;

/// <summary>
/// Represents a Request Body Object in OpenAPI 3.0.
/// </summary>
public class OpenApiRequestBody
{
	/// <summary>
	/// A brief description of the request body.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// The content of the request body.
	/// </summary>
	public IDictionary<string, OpenApiMediaType>? Content { get; set; }

	/// <summary>
	/// Determines if the request body is required in the request.
	/// </summary>
	public bool? Required { get; set; }
}
