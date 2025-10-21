namespace Facility.Definition.Swagger;

/// <summary>
/// Represents a Server Variable Object in OpenAPI 3.0.
/// </summary>
public class OpenApiServerVariable
{
	/// <summary>
	/// An enumeration of string values to be used if the substitution options are from a limited set.
	/// </summary>
	public IList<string>? Enum { get; set; }

	/// <summary>
	/// The default value to use for substitution.
	/// </summary>
	public string? Default { get; set; }

	/// <summary>
	/// An optional description for the server variable.
	/// </summary>
	public string? Description { get; set; }
}
