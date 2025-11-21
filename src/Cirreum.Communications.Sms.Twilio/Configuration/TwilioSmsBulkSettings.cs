namespace Cirreum.Communications.Sms.Configuration;

public class TwilioSmsBulkSettings {
	/// <summary>
	/// The max number of concurrent SMS operations during bulk sending
	/// </summary>
	public int MaxConcurrency { get; set; } = 10;
}