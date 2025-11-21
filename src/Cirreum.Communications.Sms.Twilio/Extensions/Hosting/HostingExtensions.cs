namespace Microsoft.Extensions.Hosting;

using Cirreum.Communications.Sms;
using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Health;
using Microsoft.Extensions.DependencyInjection;

public static class HostingExtensions {

	/// <summary>
	/// Adds a manually configured <see cref="ISmsService"/> instance for Twilio Sms.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="settings">The configured instance settings.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddTwilioSmsClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		TwilioSmsInstanceSettings settings,
		Action<TwilioSmsHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		// Configure health options
		settings.HealthOptions ??= new TwilioSmsHealthCheckOptions();
		configureHealthCheckOptions?.Invoke(settings.HealthOptions);

		// Reuse our Registrar...
		var registrar = new TwilioSmsRegistrar();
		registrar.RegisterInstance(
			serviceKey,
			settings,
			builder.Services,
			builder.Configuration);

		return builder;

	}

	/// <summary>
	/// Adds a manually configured <see cref="ISmsService"/> instance for Twilio Sms.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="configure">The callback to configure the instance settings.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddTwilioSmsClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		Action<TwilioSmsInstanceSettings> configure,
		Action<TwilioSmsHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new TwilioSmsInstanceSettings();
		configure?.Invoke(settings);
		if (string.IsNullOrWhiteSpace(settings.Name)) {
			settings.Name = serviceKey;
		}

		return AddTwilioSmsClient(builder, serviceKey, settings, configureHealthCheckOptions);

	}

	/// <summary>
	/// Adds a manually configured <see cref="ISmsService"/> instance for Twilio Sms.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="twilioConfiguration">The JSON string containing accountSid, authToken, and optional serviceId/from settings.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddTwilioSmsClient(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string twilioConfiguration,
		Action<TwilioSmsHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new TwilioSmsInstanceSettings() {
			ConnectionString = twilioConfiguration,
			Name = serviceKey
		};

		return AddTwilioSmsClient(builder, serviceKey, settings, configureHealthCheckOptions);

	}

}