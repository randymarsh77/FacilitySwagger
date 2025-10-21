using System.Text.RegularExpressions;
using Facility.Definition.CodeGen;
using Facility.Definition.Fsd;
using Newtonsoft.Json;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Facility.Definition.Swagger;

/// <summary>
/// Parses OpenAPI 3.0.
/// </summary>
public sealed class OpenApiParser : ServiceParser
{
	/// <summary>
	/// The service name (defaults to 'info/x-identifier' or 'info/title').
	/// </summary>
	public string? ServiceName { get; set; }

	/// <summary>
	/// Implements TryParseDefinition.
	/// </summary>
	protected override bool TryParseDefinitionCore(ServiceDefinitionText text, out ServiceInfo? service, out IReadOnlyList<ServiceDefinitionError> errors)
	{
		var isFsd = new FsdParser(new() { SupportsEvents = true }).TryParseDefinition(text, out service, out errors);
		if (isFsd || text.Name.EndsWith(".fsd", StringComparison.OrdinalIgnoreCase))
			return isFsd;

		service = null;

		if (string.IsNullOrWhiteSpace(text.Text))
		{
			errors = [new ServiceDefinitionError("Service definition is missing.", new ServiceDefinitionPosition(text.Name, 1, 1))];
			return false;
		}

		// Normalize YAML: replace empty flow-style mappings with null to avoid deserialization issues
		// Use regex to handle indentation
		var normalizedText = Regex.Replace(
			text.Text,
			@"(\s+)properties:\s*\{\s*\}",
			"$1properties:",
			RegexOptions.Multiline);

		OpenApiDocument openApiDocument;
		SwaggerParserContext context;

		if (!s_detectJsonRegex.IsMatch(normalizedText))
		{
			// parse YAML
			var yamlDeserializer = new DeserializerBuilder()
				.IgnoreUnmatchedProperties()
				.WithNamingConvention(new OurNamingConvention())
				.WithTypeConverter(new JTokenYamlConverter())
				.Build();
			using (var stringReader = new StringReader(normalizedText))
			{
				try
				{
					openApiDocument = yamlDeserializer.Deserialize<OpenApiDocument>(stringReader);
				}
				catch (YamlException exception)
				{
					var errorMessage = exception.InnerException?.Message ?? exception.Message;
					const string errorStart = "): ";
					var errorStartIndex = errorMessage.IndexOf(errorStart, StringComparison.OrdinalIgnoreCase);
					if (errorStartIndex != -1)
						errorMessage = errorMessage.Substring(errorStartIndex + errorStart.Length);

					errors = [new ServiceDefinitionError(errorMessage, new ServiceDefinitionPosition(text.Name, exception.End.Line, exception.End.Column))];
					return false;
				}
			}

			if (openApiDocument == null)
			{
				errors = [new ServiceDefinitionError("Service definition is missing.", new ServiceDefinitionPosition(text.Name, 1, 1))];
				return false;
			}

			context = SwaggerParserContext.FromYaml(text);
		}
		else
		{
			// parse JSON
			using (var stringReader = new StringReader(text.Text))
			using (var jsonTextReader = new JsonTextReader(stringReader))
			{
				try
				{
					openApiDocument = JsonSerializer.Create(OpenApiUtility.JsonSerializerSettings).Deserialize<OpenApiDocument>(jsonTextReader)!;
				}
				catch (JsonException exception)
				{
					errors = [new ServiceDefinitionError(exception.Message, new ServiceDefinitionPosition(text.Name, jsonTextReader.LineNumber, jsonTextReader.LinePosition))];
					return false;
				}

				context = SwaggerParserContext.FromJson(text);
			}
		}

		var conversion = OpenApiConversion.Create(openApiDocument, ServiceName, context);
		service = conversion.Service;
		errors = conversion.Errors;
		return errors.Count == 0;
	}

	/// <summary>
	/// Converts OpenAPI 3.0 into a service definition.
	/// </summary>
	/// <exception cref="ServiceDefinitionException">Thrown if the service would be invalid.</exception>
	public ServiceInfo ConvertOpenApiDocument(OpenApiDocument openApiDocument)
	{
		if (TryConvertOpenApiDocument(openApiDocument, out var service, out var errors))
			return service!;
		else
			throw new ServiceDefinitionException(errors);
	}

	/// <summary>
	/// Attempts to convert OpenAPI 3.0 into a service definition.
	/// </summary>
	public bool TryConvertOpenApiDocument(OpenApiDocument openApiDocument, out ServiceInfo? service, out IReadOnlyList<ServiceDefinitionError> errors)
	{
		var conversion = OpenApiConversion.Create(openApiDocument, ServiceName, SwaggerParserContext.None);
		service = conversion.Service;
		errors = conversion.Errors;
		return errors.Count == 0;
	}

	private sealed class OurNamingConvention : INamingConvention
	{
		public string Apply(string value)
		{
			if (value[0] >= 'A' && value[0] <= 'Z')
				value = CodeGenUtility.ToCamelCase(value);
			return value;
		}
	}

	private static readonly Regex s_detectJsonRegex = new Regex(@"^\s*[{/]", RegexOptions.Singleline);
}
