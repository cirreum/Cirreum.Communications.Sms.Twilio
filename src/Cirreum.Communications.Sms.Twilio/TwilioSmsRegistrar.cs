namespace Cirreum.Communications.Sms;

using Cirreum.Communications.Sms.Configuration;
using Cirreum.Communications.Sms.Extensions;
using Cirreum.Communications.Sms.Health;
using Cirreum.Providers;
using Cirreum.ServiceProvider;
using Cirreum.ServiceProvider.Configuration;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registrar responsible for auto-registering any configured sms services for the
/// 'Twilio' Sms Service Providers in the Communications section of application settings.
/// </summary>
public sealed class TwilioSmsRegistrar() :
	ServiceProviderRegistrar<
		TwilioSmsSettings,
		TwilioSmsInstanceSettings,
		TwilioSmsHealthCheckOptions> {

	/// <inheritdoc/>
	public override ProviderType ProviderType => ProviderType.Communications;

	/// <inheritdoc/>
	public override string ProviderName => "Twilio";

	/// <inheritdoc/>
	public override string[] ActivitySourceNames { get; } = [$"{typeof(TwilioClient).Namespace}.*"];

	/// <inheritdoc/>
	public override void ValidateSettings(TwilioSmsInstanceSettings settings) {
		if (string.IsNullOrWhiteSpace(settings.AccountSid)) {
			throw new InvalidOperationException("Twilio AccountSid is required");
		}
		if (string.IsNullOrWhiteSpace(settings.AuthToken)) {
			throw new InvalidOperationException("Twilio AuthToken is required");
		}
		if (string.IsNullOrWhiteSpace(settings.ServiceId) &&
			string.IsNullOrWhiteSpace(settings.From)) {
			throw new InvalidOperationException("Twilio ServiceId or From is required");
		}
	}

	/// <inheritdoc/>
	public override void Register(TwilioSmsSettings providerSettings, IServiceCollection services, IConfiguration configuration) {

		//
		// Perform normal registration process first
		//
		base.Register(providerSettings, services, configuration);

		// Then get the 'default' or first configured instance
		var primaryInstance = GetDefaultOrFirstInstance(providerSettings);
		if (primaryInstance == null) {
			return;
		}

		// Init the Singleton Client
		TwilioClient.Init(primaryInstance.AccountSid, primaryInstance.AuthToken);

		// Set region if provided
		if (!string.IsNullOrWhiteSpace(primaryInstance.Region)) {
			TwilioClient.SetRegion(primaryInstance.Region);
		}

		// Set edge if provided
		if (!string.IsNullOrWhiteSpace(primaryInstance.Edge)) {
			TwilioClient.SetEdge(primaryInstance.Edge);
		}

	}

	/// <inheritdoc/>
	protected override void AddServiceProviderInstance(
		IServiceCollection services,
		string serviceKey,
		TwilioSmsInstanceSettings settings) {
		services.AddTwilioSmsService(serviceKey, settings);
	}

	/// <inheritdoc/>
	protected override IServiceProviderHealthCheck<TwilioSmsHealthCheckOptions> CreateHealthCheck(
		IServiceProvider serviceProvider,
		string serviceKey,
		TwilioSmsInstanceSettings settings) {
		return serviceProvider.CreateTwilioSmsHealthCheck(serviceKey, settings);
	}

	private static TwilioSmsInstanceSettings? GetDefaultOrFirstInstance(TwilioSmsSettings providerSettings) {
		if (providerSettings.Instances.Count == 0) {
			return null;
		}
		return providerSettings.Instances
			.FirstOrDefault(kvp => kvp.Key.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase))
			.Value ?? providerSettings.Instances.First().Value;
	}

}