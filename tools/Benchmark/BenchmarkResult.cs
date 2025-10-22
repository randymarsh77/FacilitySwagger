// Copyright (c) Facility Contributors

namespace Benchmark;

/// <summary>
/// Result of processing a single spec file.
/// </summary>
internal sealed class BenchmarkResult
{
	/// <summary>
	/// Gets or sets the file name.
	/// </summary>
	public string FileName { get; set; } = "";

	/// <summary>
	/// Gets or sets a value indicating whether the conversion succeeded.
	/// </summary>
	public bool Success { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether the file was skipped.
	/// </summary>
	public bool Skipped { get; set; }

	/// <summary>
	/// Gets or sets the error message if conversion failed.
	/// </summary>
	public string? ErrorMessage { get; set; }

	/// <summary>
	/// Gets or sets the elapsed time in milliseconds.
	/// </summary>
	public double ElapsedMs { get; set; }

	/// <summary>
	/// Gets or sets the number of methods found.
	/// </summary>
	public int MethodCount { get; set; }

	/// <summary>
	/// Gets or sets the number of DTOs found.
	/// </summary>
	public int DtoCount { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether this is OpenAPI v3.
	/// </summary>
	public bool IsOpenApiV3 { get; set; }
}
