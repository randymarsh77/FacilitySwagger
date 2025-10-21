namespace Facility.Definition.Swagger;

/// <summary>
/// Represents the Components Object in OpenAPI 3.0.
/// </summary>
public class OpenApiComponents
{
	/// <summary>
	/// An object to hold reusable Schema Objects.
	/// </summary>
	public IDictionary<string, SwaggerSchema>? Schemas { get; set; }

	/// <summary>
	/// An object to hold reusable Response Objects.
	/// </summary>
	public IDictionary<string, SwaggerResponse>? Responses { get; set; }

	/// <summary>
	/// An object to hold reusable Parameter Objects.
	/// </summary>
	public IDictionary<string, SwaggerParameter>? Parameters { get; set; }

	/// <summary>
	/// An object to hold reusable Request Body Objects.
	/// </summary>
	public IDictionary<string, OpenApiRequestBody>? RequestBodies { get; set; }

	/// <summary>
	/// An object to hold reusable Security Scheme Objects.
	/// </summary>
	public IDictionary<string, SwaggerSecurityScheme>? SecuritySchemes { get; set; }
}
