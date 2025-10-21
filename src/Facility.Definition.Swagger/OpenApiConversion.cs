namespace Facility.Definition.Swagger;

internal sealed class OpenApiConversion
{
	public static OpenApiConversion Create(OpenApiDocument openApiDocument, string? serviceName, SwaggerParserContext context)
	{
		var conversion = new OpenApiConversion(openApiDocument, serviceName);
		conversion.Convert(context);
		return conversion;
	}

	public ServiceInfo? Service { get; private set; }

	public IReadOnlyList<ServiceDefinitionError> Errors => m_errors;

	private OpenApiConversion(OpenApiDocument openApiDocument, string? serviceName)
	{
		m_openApiDocument = openApiDocument;
		m_serviceName = serviceName;
		m_errors = new List<ServiceDefinitionError>();
	}

	private void Convert(SwaggerParserContext context)
	{
		if (m_openApiDocument.OpenApi == null)
			m_errors.Add(context.CreateError("openapi field is missing."));
		else if (!m_openApiDocument.OpenApi.StartsWith("3.0", StringComparison.Ordinal) &&
			!m_openApiDocument.OpenApi.StartsWith("3.1", StringComparison.Ordinal))
			m_errors.Add(context.CreateError("openapi should start with '3.0' or '3.1'.", "openapi"));

		if (m_openApiDocument.Info == null)
			m_errors.Add(context.CreateError("info is missing."));

		// Convert OpenAPI 3.0 to Swagger 2.0 format for reuse of existing conversion logic
		var swaggerService = ConvertToSwaggerService();

		// Use existing Swagger conversion logic
		var swaggerConversion = SwaggerConversion.Create(swaggerService, m_serviceName, context);
		Service = swaggerConversion.Service;
		m_errors.AddRange(swaggerConversion.Errors);
	}

	private SwaggerService ConvertToSwaggerService()
	{
		var swaggerService = new SwaggerService
		{
			Swagger = "2.0",
			Info = m_openApiDocument.Info,
			Paths = m_openApiDocument.Paths,
			Tags = m_openApiDocument.Tags,
			ExternalDocs = m_openApiDocument.ExternalDocs,
			Security = m_openApiDocument.Security,
		};

		// Convert servers to host/basePath/schemes
		if (m_openApiDocument.Servers != null && m_openApiDocument.Servers.Count > 0)
		{
			var firstServer = m_openApiDocument.Servers[0];
			if (firstServer.Url != null)
			{
				// Replace server variables with their default values
				var serverUrl = firstServer.Url;
				if (firstServer.Variables != null)
				{
					foreach (var variable in firstServer.Variables)
					{
						var placeholder = $"{{{variable.Key}}}";
#pragma warning disable CA2249 // Use string.Contains instead of string.IndexOf (not available in netstandard2.0)
						if (serverUrl.IndexOf(placeholder, StringComparison.Ordinal) >= 0)
#pragma warning restore CA2249
							serverUrl = serverUrl.ReplaceOrdinal(placeholder, variable.Value.Default ?? "example.com");
					}
				}

				try
				{
					var uri = new Uri(serverUrl, UriKind.RelativeOrAbsolute);
					if (uri.IsAbsoluteUri)
					{
						swaggerService.Host = uri.Host;
						swaggerService.Schemes = [uri.Scheme];
						if (uri.PathAndQuery != "/" && !string.IsNullOrEmpty(uri.PathAndQuery))
							swaggerService.BasePath = uri.PathAndQuery;
					}
					else
					{
						swaggerService.BasePath = serverUrl;
					}
				}
				catch (UriFormatException)
				{
					// If URI parsing fails, use the URL as basePath
					swaggerService.BasePath = serverUrl;
				}
			}
		}

		// Convert components to definitions/parameters/responses/securityDefinitions
		if (m_openApiDocument.Components != null)
		{
			swaggerService.Definitions = m_openApiDocument.Components.Schemas;
			swaggerService.Parameters = m_openApiDocument.Components.Parameters;
			swaggerService.Responses = ConvertComponentResponses(m_openApiDocument.Components.Responses);
			swaggerService.SecurityDefinitions = m_openApiDocument.Components.SecuritySchemes;

			// Convert all refs in component schemas
			if (swaggerService.Definitions != null)
			{
				foreach (var schema in swaggerService.Definitions.Values)
					ConvertSchemaRefs(schema);
			}

			// Convert all refs in component parameters
			if (swaggerService.Parameters != null)
			{
				foreach (var parameter in swaggerService.Parameters.Values)
				{
					if (parameter.Schema != null)
						ConvertSchemaRefs(parameter.Schema);
				}
			}
		}

		// Convert requestBody and response content to Swagger 2.0 format
		if (m_openApiDocument.Paths != null)
		{
			foreach (var pathPair in m_openApiDocument.Paths)
			{
				var operations = pathPair.Value;

				// Convert path-level parameter refs
				if (operations.Parameters != null)
				{
					foreach (var parameter in operations.Parameters)
					{
						if (parameter.Ref != null && parameter.Ref.StartsWith("#/components/parameters/", StringComparison.Ordinal))
							parameter.Ref = parameter.Ref.ReplaceOrdinal("#/components/parameters/", "#/parameters/");

						if (parameter.Schema != null)
							ConvertSchemaRefs(parameter.Schema);
					}
				}

				ConvertOperationForSwagger(operations.Get);
				ConvertOperationForSwagger(operations.Post);
				ConvertOperationForSwagger(operations.Put);
				ConvertOperationForSwagger(operations.Delete);
				ConvertOperationForSwagger(operations.Options);
				ConvertOperationForSwagger(operations.Head);
				ConvertOperationForSwagger(operations.Patch);
			}
		}

		return swaggerService;
	}

	private void ConvertOperationForSwagger(SwaggerOperation? operation)
	{
		if (operation == null)
			return;

		// Convert parameter refs from OpenAPI 3.0 to Swagger 2.0
		if (operation.Parameters != null)
		{
			foreach (var parameter in operation.Parameters)
			{
				if (parameter.Ref != null && parameter.Ref.StartsWith("#/components/parameters/", StringComparison.Ordinal))
					parameter.Ref = parameter.Ref.ReplaceOrdinal("#/components/parameters/", "#/parameters/");

				// Also convert refs in parameter schema if present
				if (parameter.Schema != null)
					ConvertSchemaRefs(parameter.Schema);
			}
		}

		// Convert requestBody to body parameter
		if (operation.RequestBody != null)
		{
			var requestBody = operation.RequestBody;
			var schema = GetSchemaFromContent(requestBody.Content);

			if (schema != null)
			{
				// Convert OpenAPI 3.0 schema refs to Swagger 2.0 refs
				ConvertSchemaRefs(schema);

				var bodyParameter = new SwaggerParameter
				{
					In = SwaggerParameterKind.Body,
					Name = "body",
					Description = requestBody.Description,
					Required = requestBody.Required,
					Schema = schema,
				};

				operation.Parameters = operation.Parameters ?? new List<SwaggerParameter>();
				operation.Parameters.Add(bodyParameter);
			}

			// Clear requestBody since we've converted it
			operation.RequestBody = null;
		}

		// Convert response content to schema
		if (operation.Responses != null)
		{
			foreach (var responsePair in operation.Responses)
			{
				var response = responsePair.Value;

				// Convert response refs from OpenAPI 3.0 to Swagger 2.0
				if (response.Ref != null && response.Ref.StartsWith("#/components/responses/", StringComparison.Ordinal))
					response.Ref = response.Ref.ReplaceOrdinal("#/components/responses/", "#/responses/");

				if (response.Content != null && response.Schema == null)
				{
					var schema = GetSchemaFromContent(response.Content);
					if (schema != null)
					{
						ConvertSchemaRefs(schema);
						response.Schema = schema;
					}
				}
			}
		}
	}

	private SwaggerSchema? GetSchemaFromContent(IDictionary<string, OpenApiMediaType>? content)
	{
		if (content == null)
			return null;

		// Prefer application/json
		if (content.TryGetValue("application/json", out var mediaType))
			return mediaType.Schema;

		// Otherwise, use first available
		return content.Values.FirstOrDefault()?.Schema;
	}

	private IDictionary<string, SwaggerResponse>? ConvertComponentResponses(IDictionary<string, SwaggerResponse>? responses)
	{
		if (responses == null)
			return null;

		foreach (var responsePair in responses)
		{
			var response = responsePair.Value;
			if (response.Content != null && response.Schema == null)
			{
				var schema = GetSchemaFromContent(response.Content);
				if (schema != null)
				{
					ConvertSchemaRefs(schema);
					response.Schema = schema;
				}
			}
		}

		return responses;
	}

	private void ConvertSchemaRefs(SwaggerSchema schema)
	{
		if (schema.Ref != null)
		{
			if (schema.Ref.StartsWith("#/components/schemas/", StringComparison.Ordinal))
			{
				schema.Ref = schema.Ref.ReplaceOrdinal("#/components/schemas/", "#/definitions/");
			}
			else if (schema.Ref.StartsWith("#/components/parameters/", StringComparison.Ordinal))
			{
				schema.Ref = schema.Ref.ReplaceOrdinal("#/components/parameters/", "#/parameters/");
			}
			else if (schema.Ref.StartsWith("#/components/responses/", StringComparison.Ordinal))
			{
				schema.Ref = schema.Ref.ReplaceOrdinal("#/components/responses/", "#/responses/");
			}
		}

		if (schema.Items != null)
			ConvertSchemaRefs(schema.Items);

		if (schema.AdditionalProperties is SwaggerSchema additionalPropertiesSchema)
			ConvertSchemaRefs(additionalPropertiesSchema);

		if (schema.Properties != null)
		{
			foreach (var property in schema.Properties.Values)
				ConvertSchemaRefs(property);
		}

		if (schema.AllOf != null)
		{
			foreach (var allOfSchema in schema.AllOf)
				ConvertSchemaRefs(allOfSchema);
		}
	}

	private readonly OpenApiDocument m_openApiDocument;
	private readonly string? m_serviceName;
	private readonly List<ServiceDefinitionError> m_errors;
}
