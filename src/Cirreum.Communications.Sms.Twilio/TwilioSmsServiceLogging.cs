namespace Cirreum.Communications.Sms;

using Microsoft.Extensions.Logging;
using System;

internal static partial class TwilioSmsServiceLogging {

	[LoggerMessage(
		EventId = 1001,
		Level = LogLevel.Information,
		Message = "{Header}: Sending to {To} from {From}, message length: {Length}")]
	public static partial void LogSendingFromMessage(this ILogger logger, string header, string to, string from, int length);

	[LoggerMessage(
		EventId = 1002,
		Level = LogLevel.Information,
		Message = "{Header}: Sending to {To} via messaging service {ServiceId}, message length: {Length}")]
	public static partial void LogSendingViaServiceMessage(this ILogger logger, string header, string to, string serviceId, int length);

	[LoggerMessage(
		EventId = 1003,
		Level = LogLevel.Error,
		Message = "{Header} Error sending message")]
	public static partial void LogErrorSendingMessage(this ILogger logger, string header, Exception ex);

	[LoggerMessage(
		EventId = 1004,
		Level = LogLevel.Error,
		Message = "Error processing phone number {PhoneNumber}")]
	public static partial void LogErrorProcessingPhoneNumber(this ILogger logger, Exception ex, string phoneNumber);

	[LoggerMessage(
		EventId = 1005,
		Level = LogLevel.Warning,
		Message = "429 from Twilio for {Target}. Retrying in {DelayMs} ms (attempt {Attempt}/{Max}). Code={Code} Status={Status}")]
	public static partial void LogRateLimitRetry(this ILogger logger, string target, int delayMs, int attempt, int max, int? code, int? status);

	[LoggerMessage(
		EventId = 1006,
		Level = LogLevel.Error,
		Message = "Non-retryable error sending to {Target}")]
	public static partial void LogNonRetryableError(this ILogger logger, Exception ex, string target);

	[LoggerMessage(
		EventId = 1007,
		Level = LogLevel.Error,
		Message = "{Header} Failed - no result returned")]
	public static partial void LogNoResultReturned(this ILogger logger, string header);

	[LoggerMessage(
		EventId = 1008,
		Level = LogLevel.Error,
		Message = "{Header} Failed with status: {Status}, ErrorMessage: {ErrorMessage}")]
	public static partial void LogFailedWithStatus(this ILogger logger, string header, string status, string errorMessage);

	[LoggerMessage(
		EventId = 1009,
		Level = LogLevel.Information,
		Message = "{Header} Success. MessageSid: {MessageSid}")]
	public static partial void LogSuccess(this ILogger logger, string header, string messageSid);

	[LoggerMessage(
		EventId = 1010,
		Level = LogLevel.Warning,
		Message = "Invalid phone number: {PhoneNumber}")]
	public static partial void LogInvalidPhoneNumber(this ILogger logger, string phoneNumber);

	[LoggerMessage(
		EventId = 1011,
		Level = LogLevel.Error,
		Message = "Error parsing phone number: {PhoneNumber}")]
	public static partial void LogErrorParsingPhoneNumber(this ILogger logger, Exception ex, string phoneNumber);

}