// Copyright (c) Facility Contributors

using System.Diagnostics;
using Facility.Definition;
using Facility.Definition.Swagger;

namespace Benchmark;

/// <summary>
/// Benchmark tool for testing OpenAPI spec conversions.
/// </summary>
public sealed class Program
{
	/// <summary>
	/// Main entry point.
	/// </summary>
	public static async Task<int> Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.WriteLine("Usage: benchmark <path-to-specs-directory>");
			Console.WriteLine("Example: benchmark /path/to/bigcommerce-docs/reference");
			return 1;
		}

		var specsDirectory = args[0];
		if (!Directory.Exists(specsDirectory))
		{
			Console.Error.WriteLine($"Directory not found: {specsDirectory}");
			return 1;
		}

		Console.WriteLine("FacilitySwagger Benchmark Tool");
		Console.WriteLine("==============================");
		Console.WriteLine($"Scanning directory: {specsDirectory}");
		Console.WriteLine();

		// Find all YAML and JSON files
		var files = Directory.GetFiles(specsDirectory, "*.yml", SearchOption.AllDirectories)
			.Concat(Directory.GetFiles(specsDirectory, "*.yaml", SearchOption.AllDirectories))
			.Concat(Directory.GetFiles(specsDirectory, "*.json", SearchOption.AllDirectories))
			.ToList();

		Console.WriteLine($"Found {files.Count} potential spec files");
		Console.WriteLine();

		var results = new List<BenchmarkResult>();
		var totalStopwatch = Stopwatch.StartNew();

		foreach (var file in files)
		{
			var result = await ProcessSpecFile(file).ConfigureAwait(false);
			results.Add(result);

			// Print result
			var status = result.Success ? "✓" : "✗";
			var color = result.Success ? ConsoleColor.Green : ConsoleColor.Red;

			Console.ForegroundColor = color;
			Console.Write(status);
			Console.Write(" ");
			Console.ResetColor();
			Console.Write(Path.GetFileName(file));
			Console.Write(" ");

			if (result.Success)
			{
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.Write($"({result.ElapsedMs:F0}ms, ");
				Console.Write($"{result.MethodCount} methods, ");
				Console.Write($"{result.DtoCount} DTOs)");
				Console.ResetColor();
			}
			else
			{
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.Write($"({result.ErrorMessage})");
				Console.ResetColor();
			}

			Console.WriteLine();
		}

		totalStopwatch.Stop();

		// Print summary
		Console.WriteLine();
		Console.WriteLine("Summary");
		Console.WriteLine("=======");

		var successful = results.Count(r => r.Success);
		var failed = results.Count(r => !r.Success);
		var skipped = results.Count(r => r.Skipped);
		var openApiV3 = results.Count(r => r.Success && r.IsOpenApiV3);
		var swaggerV2 = results.Count(r => r.Success && !r.IsOpenApiV3);

		Console.WriteLine($"Total files processed: {results.Count}");
		Console.WriteLine($"Successful conversions: {successful} (OpenAPI v3: {openApiV3}, Swagger v2: {swaggerV2})");
		Console.WriteLine($"Failed conversions: {failed}");
		Console.WriteLine($"Skipped (not OpenAPI): {skipped}");
		Console.WriteLine($"Total time: {totalStopwatch.ElapsedMilliseconds:N0}ms");

		if (successful > 0)
		{
			var avgTime = results.Where(r => r.Success).Average(r => r.ElapsedMs);
			var totalMethods = results.Where(r => r.Success).Sum(r => r.MethodCount);
			var totalDtos = results.Where(r => r.Success).Sum(r => r.DtoCount);

			Console.WriteLine($"Average conversion time: {avgTime:F0}ms");
			Console.WriteLine($"Total methods: {totalMethods}");
			Console.WriteLine($"Total DTOs: {totalDtos}");
		}

		Console.WriteLine();

		if (failed > 0)
		{
			Console.WriteLine("Failed files:");
			Console.WriteLine("=============");
			foreach (var result in results.Where(r => !r.Success && !r.Skipped))
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"  {Path.GetFileName(result.FileName)}");
				Console.ResetColor();
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine($"    {result.ErrorMessage}");
				Console.ResetColor();
			}
		}

		return failed > 0 ? 1 : 0;
	}

	private static async Task<BenchmarkResult> ProcessSpecFile(string filePath)
	{
		var result = new BenchmarkResult
		{
			FileName = filePath,
		};

		try
		{
			var content = await File.ReadAllTextAsync(filePath).ConfigureAwait(false);

			// Quick check if this looks like an OpenAPI/Swagger file
			if (!content.Contains("openapi:", StringComparison.Ordinal) && !content.Contains("swagger:", StringComparison.Ordinal) &&
				!content.Contains("\"openapi\"", StringComparison.Ordinal) && !content.Contains("\"swagger\"", StringComparison.Ordinal))
			{
				result.Skipped = true;
				result.ErrorMessage = "Not an OpenAPI/Swagger spec";
				return result;
			}

			var stopwatch = Stopwatch.StartNew();

			var parser = new SwaggerParser();
			var serviceText = new ServiceDefinitionText(Path.GetFileName(filePath), content);

			ServiceInfo service;
			try
			{
				service = parser.ParseDefinition(serviceText);
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (ServiceDefinitionException ex)
			{
				stopwatch.Stop();
				result.Success = false;
				result.ErrorMessage = ex.Errors.FirstOrDefault()?.Message ?? ex.Message;
				result.ElapsedMs = stopwatch.ElapsedMilliseconds;
				return result;
			}

			stopwatch.Stop();

			result.Success = true;
			result.ElapsedMs = stopwatch.ElapsedMilliseconds;
			result.MethodCount = service.Methods.Count;
			result.DtoCount = service.Dtos.Count;
			result.IsOpenApiV3 = content.Contains("openapi:", StringComparison.Ordinal) || content.Contains("\"openapi\"", StringComparison.Ordinal);

			return result;
		}
		catch (Exception ex)
		{
			result.Success = false;
			result.ErrorMessage = $"{ex.Message}\n{ex.StackTrace}";
			if (ex.InnerException != null)
			{
				result.ErrorMessage += $"\nInner: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}";
			}

			return result;
		}
#pragma warning restore CA1031 // Do not catch general exception types
	}
}
