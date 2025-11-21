namespace Cirreum.Communications.Sms.Configuration;

using Cirreum.Communications.Sms.Health;
using Cirreum.ServiceProvider.Configuration;
using System.Text.Json;

public class TwilioSmsInstanceSettings :
	ServiceProviderInstanceSettings<
		TwilioSmsHealthCheckOptions> {

	/// <summary>
	/// The Sid of the account to use.
	/// </summary>
	public string AccountSid { get; set; } = "";
	/// <summary>
	/// The authorization token, of the account being used.
	/// </summary>
	public string AuthToken { get; set; } = "";
	/// <summary>
	/// The optional service id used to send an Sms message (takes priority over the <see cref="From"/> value).
	/// </summary>
	public string ServiceId { get; set; } = "";
	/// <summary>
	/// The optional From phone number used to send an Sms message (is ignored if <see cref="ServiceId"/> has a value).
	/// </summary>
	public string From { get; set; } = "";
	/// <summary>
	/// The Twilio Edge location.
	/// </summary>
	public string Edge { get; set; } = "umatilla";
	/// <summary>
	/// The Twilio Region.
	/// </summary>
	public string Region { get; set; } = "us1";
	/// <summary>
	/// The maximum number of times to retry a message when sending fails
	/// </summary>
	public int MaxRetries { get; set; } = 3;
	/// <summary>
	/// Bulk sending options.
	/// </summary>
	public TwilioSmsBulkSettings BulkOptions { get; set; } = new();


	/// <summary>
	/// Overrides the base health check options with Twilio-specific settings.
	/// </summary>
	public override TwilioSmsHealthCheckOptions? HealthOptions { get; set; }
		= new TwilioSmsHealthCheckOptions();


	public override void ParseConnectionString(string jsonValue) {

		this.ConnectionString = jsonValue;

		try {

			var options =
				JsonSerializer.Deserialize<TwilioConnectionData>(jsonValue, JsonSerializerOptions.Web)
				?? throw new InvalidOperationException("invalid Twilio configuration data.");

			this.AccountSid = options.AccountSid ?? throw new InvalidOperationException("Missing Twilio AccountSid");
			this.AuthToken = options.AuthToken ?? throw new InvalidOperationException("Missing Twilio AuthToken");

			// Only set from Key Vault if current value is empty
			if (string.IsNullOrWhiteSpace(this.ServiceId)) {
				this.ServiceId = options.ServiceId ?? "";
			} else if (this.ServiceId.Equals("none", StringComparison.OrdinalIgnoreCase)) {
				this.ServiceId = "";
			}

			if (string.IsNullOrWhiteSpace(this.From)) {
				// Not set in appsettings → use Key Vault value
				this.From = options.From ?? "";
			} else if (this.From.Equals("none", StringComparison.OrdinalIgnoreCase)) {
				// Set to "none" in appsettings → keep it empty (ignore Key Vault)
				this.From = "";
			}

		} catch (JsonException ex) {
			throw new InvalidOperationException("Invalid Twilio configuration format", ex);
		}
	}

	private record TwilioConnectionData(
		string AccountSid,
		string AuthToken,
		string ServiceId = "",
		string From = "");

}