namespace Cirreum.Communications.Sms.Extensions;

using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Health;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal static class SmsRegistrationExtensions {

	public static void AddTwilioSmsService(
		this IServiceCollection services,
		string serviceKey,
		TwilioSmsInstanceSettings settings) {

		// Register Keyed Service Factory
		services.AddKeyedSingleton<ISmsService>(
			serviceKey,
			(sp, key) => sp.CreateTwilioSmsClient(settings));

		// Register Default (non-Keyed) Service Factory (wraps the keyed registration)
		if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
			services.TryAddSingleton(sp => sp.GetRequiredKeyedService<ISmsService>(serviceKey));
		}

	}

	private static TwilioSmsService CreateTwilioSmsClient(
		this IServiceProvider serviceProvider,
		TwilioSmsInstanceSettings settings) {

		var logger = serviceProvider.GetRequiredService<ILogger<TwilioSmsService>>();
		return new TwilioSmsService(
			settings,
			logger);

	}

	public static TwilioSmsHealthCheck CreateTwilioSmsHealthCheck(
		this IServiceProvider serviceProvider,
		string serviceKey,
		TwilioSmsInstanceSettings settings) {
		var env = serviceProvider.GetRequiredService<IHostEnvironment>();
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var client = serviceProvider.GetRequiredKeyedService<ISmsService>(serviceKey);
		return new TwilioSmsHealthCheck(
			client,
			env.IsProduction(),
			cache,
			settings);
	}

}