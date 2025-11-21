namespace Cirreum.Communications.Sms.Health;

using Cirreum.Communications.Sms.Configuration;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

internal class TwilioSmsHealthCheck
	: IServiceProviderHealthCheck<TwilioSmsHealthCheckOptions>
	, IDisposable {

	private readonly ISmsService _smsService;
	private readonly bool _isProduction;
	private readonly IMemoryCache _memoryCache;
	private readonly TwilioSmsInstanceSettings _settings;
	private readonly TwilioSmsHealthCheckOptions _options;
	private readonly string _cacheKey;
	private readonly TimeSpan _cacheDuration;
	private readonly TimeSpan _failureCacheDuration;
	private readonly bool _cacheDisabled;
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public TwilioSmsHealthCheck(
		ISmsService smsService,
		bool isProduction,
		IMemoryCache memoryCache,
		TwilioSmsInstanceSettings settings) {

		this._smsService = smsService;
		this._isProduction = isProduction;
		this._memoryCache = memoryCache;
		this._settings = settings;
		this._options = settings.HealthOptions ?? new();

		ArgumentException.ThrowIfNullOrWhiteSpace(settings.Name);
		ArgumentException.ThrowIfNullOrWhiteSpace(this._options.PhoneNumber);

		this._cacheKey = $"_twilio_sms_health_{settings.Name.ToLowerInvariant()}";
		this._cacheDuration = this._options.CachedResultTimeout ?? TimeSpan.FromSeconds(60);
		this._failureCacheDuration = TimeSpan.FromSeconds(Math.Max(35, (this._options.CachedResultTimeout ?? TimeSpan.FromSeconds(60)).TotalSeconds / 2));
		this._cacheDisabled = (this._options.CachedResultTimeout is null || this._options.CachedResultTimeout.Value.TotalSeconds == 0);

	}

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default) {

		if (this._cacheDisabled) {
			// No caching...
			return await this.CheckTwilioSmsHealthAsync(context, cancellationToken)
				.ConfigureAwait(false);
		}

		// Try get from cache first
		if (this._memoryCache.TryGetValue(this._cacheKey, out HealthCheckResult cachedResult)) {
			return cachedResult;
		}

		// If not in cache, ensure only one thread updates it
		try {

			await this._semaphore.WaitAsync(cancellationToken);

			// Double-check after acquiring semaphore
			if (this._memoryCache.TryGetValue(this._cacheKey, out cachedResult)) {
				return cachedResult;
			}

			// Perform actual health check
			var result = await this.CheckTwilioSmsHealthAsync(context, cancellationToken)
				.ConfigureAwait(false);

			// Cache with appropriate duration based on health status
			var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 5));
			var duration = result.Status == HealthStatus.Healthy
				? this._cacheDuration
				: this._failureCacheDuration;

			return this._memoryCache.Set(this._cacheKey, result, duration + jitter);

		} finally {
			this._semaphore.Release();
		}
	}

	private async Task<HealthCheckResult> CheckTwilioSmsHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default) {

		try {

			// Always test parsing and validation (no API calls, no cost)
			var testNumbers = new[] { this._options.PhoneNumber };
			var result = await this._smsService.SendBulkAsync(
				message: "Health check test",
				phoneNumbers: testNumbers,
				validateOnly: true,
				cancellationToken: cancellationToken);
			if (result.Sent == 0 && result.Failed > 0) {
				var errorMessage = (result.Results.Count > 0 ? result.Results[0].ErrorMessage : null)
					?? "Unknown validation error";
				return new HealthCheckResult(
					context.Registration.FailureStatus,
					$"Twilio SMS validation failed: {errorMessage}");
			}

			// If testing either sending methods, validate only what's configured
			if (this._options.TestSending) {

				var results = new List<(bool success, string method, string? error)>();
				var hasServiceId = !string.IsNullOrWhiteSpace(this._settings.ServiceId);
				var hasFromNumber = !string.IsNullOrWhiteSpace(this._settings.From);

				// Only test messaging service if it's configured
				if (hasServiceId) {
					var serviceResult = await this._smsService.SendViaServiceAsync(
						serviceId: this._settings.ServiceId,
						to: this._options.PhoneNumber,
						message: "Health check test",
						cancellationToken: cancellationToken);

					results.Add((serviceResult.Success, "MessagingService", serviceResult.ErrorMessage));
				}

				// Only test direct number if it's configured  
				if (hasFromNumber) {
					var fromResult = await this._smsService.SendFromAsync(
						from: this._settings.From,
						to: this._options.PhoneNumber,
						message: "Health check test",
						cancellationToken: cancellationToken);

					results.Add((fromResult.Success, "FromNumber", fromResult.ErrorMessage));
				}

				// If neither is configured, that's a real problem
				if (!hasServiceId && !hasFromNumber) {
					return new HealthCheckResult(
						HealthStatus.Unhealthy,
						"No ServiceId or From number configured for SMS sending");
				}

				// Check if any tests failed
				var failures = results.Where(r => !r.success).ToList();
				if (failures.Count > 0) {
					var errorDetails = string.Join("; ", failures.Select(f => $"{f.method}: {f.error}"));
					return new HealthCheckResult(
						context.Registration.FailureStatus,
						$"Twilio SMS sending test failed - {errorDetails}");
				}


				// If we get here, all configured sending methods succeeded
				if (!this._isProduction) {
					var testedMethods = string.Join(", ", results.Select(r => r.method));
					return HealthCheckResult.Healthy($"Twilio SMS service validated via {testedMethods} with test number {this._options.PhoneNumber}");
				}

			}

			if (this._isProduction) {
				return HealthCheckResult.Healthy("Twilio SMS service is operational");
			}

			return HealthCheckResult.Healthy($"Twilio SMS service is healthy but did not actually attempt to send a message.");

		} catch (HttpRequestException httpEx) {
			// Network connectivity issues
			return new HealthCheckResult(
				HealthStatus.Degraded,
				$"Twilio connectivity issue: {httpEx.Message}");
		} catch (Exception ex) {
			return new HealthCheckResult(
				context.Registration.FailureStatus,
				$"Twilio SMS health check failed: {ex.Message}",
				ex);
		}

	}

	public void Dispose() {
		this._semaphore?.Dispose();
	}

}