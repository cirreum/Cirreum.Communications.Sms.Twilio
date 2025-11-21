namespace Cirreum.Communications.Sms;

using Cirreum.Communications.Sms.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class TwilioSmsService(
	TwilioSmsInstanceSettings settings,
	ILogger<TwilioSmsService> logger
) : ISmsService {

	private const string LogHeader = "Twilio SMS";
	private readonly PhoneNumbers.PhoneNumberUtil _phoneUtil = PhoneNumbers.PhoneNumberUtil.GetInstance();
	private static readonly MessageResource.StatusEnum[] SuccessStatuses = [
		MessageResource.StatusEnum.Queued,
		MessageResource.StatusEnum.Sending,
		MessageResource.StatusEnum.Sent,
		MessageResource.StatusEnum.Delivered,
		MessageResource.StatusEnum.Accepted,
		MessageResource.StatusEnum.Scheduled
	];

	public async Task<MessageResult> SendFromAsync(
		string from,
		string to,
		string message,
		SmsOptions? options = null,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(from);
		ArgumentException.ThrowIfNullOrWhiteSpace(to);
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		logger.LogSendingFromMessage(LogHeader, to, from, message.Length);

		// Check for cancellation before expensive API call
		cancellationToken.ThrowIfCancellationRequested();

		try {

			var createOptions = new CreateMessageOptions(new PhoneNumber(to)) {
				From = new PhoneNumber(from),
				Body = message
			};

			if (options != null) {
				ApplySmsOptions(createOptions, options);
			}

			var result = await MessageResource.CreateAsync(createOptions);

			return this.ProcessMessageResult(result, to, LogHeader);

		} catch (Exception ex) {
			logger.LogErrorSendingMessage(LogHeader, ex);
			return new MessageResult(to, false, ErrorMessage: ex.Message);
		}
	}

	public async Task<MessageResult> SendViaServiceAsync(
		string serviceId,
		string to,
		string message,
		SmsOptions? options = null,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(serviceId);
		ArgumentException.ThrowIfNullOrWhiteSpace(to);
		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		logger.LogSendingViaServiceMessage(LogHeader, to, serviceId, message.Length);

		// Check for cancellation before expensive API call
		cancellationToken.ThrowIfCancellationRequested();

		try {

			var createOptions = new CreateMessageOptions(new PhoneNumber(to)) {
				MessagingServiceSid = serviceId,
				Body = message
			};

			if (options != null) {
				ApplySmsOptions(createOptions, options);
			}

			var result = await MessageResource.CreateAsync(createOptions);

			return this.ProcessMessageResult(result, to, LogHeader);

		} catch (Exception ex) {
			logger.LogErrorSendingMessage(LogHeader, ex);
			return new MessageResult(to, false, ErrorMessage: ex.Message);
		}

	}

	public async Task<MessageResponse> SendBulkAsync(
		string message,
		IEnumerable<string> phoneNumbers,
		string? from = null,
		string? serviceId = null,
		string countryCode = "US",
		bool validateOnly = false,
		SmsOptions? options = null,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(message);

		var numbers = phoneNumbers?.ToList() ?? [];
		if (numbers.Count == 0) {
			throw new ArgumentException("Phone number list cannot be empty", nameof(phoneNumbers));
		}

		// Default to configuration if not provided
		from ??= settings.From;
		serviceId ??= settings.ServiceId;

		// Validate that we have either a from number or service ID
		if (string.IsNullOrWhiteSpace(from) && string.IsNullOrWhiteSpace(serviceId)) {
			throw new InvalidOperationException(
				"Either a 'from' phone number or messaging service ID must be provided");
		}

		// Preemptively validate options and fail early
		if (options != null) {
			ValidateSmsOptions(options);
		}

		var results = new ConcurrentBag<MessageResult>();
		var sent = 0;
		var failed = 0;

		// Process phone numbers in parallel for larger batches
		// (adjust maxDegreeOfParallelism based on your needs and Twilio rate limits)
		await Parallel.ForEachAsync(
			numbers,
			new ParallelOptions {
				MaxDegreeOfParallelism = settings.BulkOptions.MaxConcurrency,
				CancellationToken = cancellationToken
			},
			async (phoneNumber, token) => {
				try {
					// Parse and validate the phone number
					var parsedNumber = this.ParsePhoneNumber(phoneNumber, countryCode);
					if (parsedNumber == null) {
						var result = new MessageResult(
							phoneNumber,
							false,
							ErrorMessage: "Invalid phone number format");
						results.Add(result);
						Interlocked.Increment(ref failed);
						return;
					}

					// If validate only, just count it as sent
					if (validateOnly) {
						results.Add(new MessageResult(parsedNumber, true));
						Interlocked.Increment(ref sent);
						return;
					}

					// Send the message using either service ID or from number
					var messageResult = !string.IsNullOrWhiteSpace(serviceId)
						? await this.SendWithRetryAsync(
							() => this.SendViaServiceAsync(serviceId, parsedNumber, message, options, token),
							target: parsedNumber,
							maxRetries: settings.MaxRetries,
							ct: token)
						: await this.SendWithRetryAsync(
							() => this.SendFromAsync(from!, parsedNumber, message, options, token),
							target: parsedNumber,
							maxRetries: settings.MaxRetries,
							ct: token);

					results.Add(messageResult);
					if (messageResult.Success) {
						Interlocked.Increment(ref sent);
					} else {
						Interlocked.Increment(ref failed);
					}

				} catch (Exception ex) {
					logger.LogErrorProcessingPhoneNumber(ex, phoneNumber);
					results.Add(new MessageResult(phoneNumber, false, ErrorMessage: ex.Message));
					Interlocked.Increment(ref failed);
				}
			});

		return new MessageResponse(sent, failed, [.. results]);
	}


	private async Task<MessageResult> SendWithRetryAsync(Func<Task<MessageResult>> sendFunc, string target, int maxRetries = 5, CancellationToken ct = default) {
		for (var attempt = 0; attempt <= maxRetries; attempt++) {
			try {
				return await sendFunc();
			} catch (Twilio.Exceptions.ApiException ex)
				  when ((ex.Status == 429 || ex.Code == 20429) && attempt < maxRetries) {

				// Exponential backoff with decorrelated jitter
				var baseDelay = TimeSpan.FromSeconds(Math.Pow(2, Math.Min(attempt, 6))); // cap growth
				var jitterMs = Random.Shared.Next(250, 1000);
				var delay = baseDelay + TimeSpan.FromMilliseconds(jitterMs);

				logger.LogRateLimitRetry(target, (int)delay.TotalMilliseconds, attempt + 1, maxRetries, ex.Code, ex.Status);

				await Task.Delay(delay, ct);
				continue;
			} catch (Exception ex) {
				logger.LogNonRetryableError(ex, target);
				return new MessageResult(target, false, ErrorMessage: ex.Message);
			}
		}

		return new MessageResult(target, false, ErrorMessage: "Rate limit exceeded, all retries exhausted");
	}
	private static void ApplySmsOptions(CreateMessageOptions createOptions, SmsOptions options) {

		if (options.ScheduledSendTime.HasValue) {
			ValidateSendTime(options.ScheduledSendTime.Value);
			createOptions.SendAt = options.ScheduledSendTime.Value;
			createOptions.ScheduleType = MessageResource.ScheduleTypeEnum.Fixed;
		}

		if (options.MediaUrls?.Any() == true) {
			ValidateMediaUrls(options.MediaUrls);
			createOptions.MediaUrl = [.. options.MediaUrls];
		}

		if (options.StatusCallbackUrl != null) {
			ValidateStatusCallbackUrl(options.StatusCallbackUrl);
			createOptions.StatusCallback = options.StatusCallbackUrl;
		}

		if (options.ValidityPeriod.HasValue) {
			// Convert TimeSpan to seconds for Twilio
			var totalSeconds = (int)options.ValidityPeriod.Value.TotalSeconds;
			ValidateValidityPeriod(totalSeconds);
			createOptions.ValidityPeriod = totalSeconds;
		}
	}
	private static void ValidateSmsOptions(SmsOptions options) {
		if (options.ScheduledSendTime.HasValue) {
			ValidateSendTime(options.ScheduledSendTime.Value);
		}
		if (options.MediaUrls?.Any() == true) {
			ValidateMediaUrls(options.MediaUrls);
		}
		if (options.StatusCallbackUrl != null) {
			ValidateStatusCallbackUrl(options.StatusCallbackUrl);
		}
		if (options.ValidityPeriod.HasValue) {
			var totalSeconds = (int)options.ValidityPeriod.Value.TotalSeconds;
			ValidateValidityPeriod(totalSeconds);
		}
	}
	private static void ValidateSendTime(DateTime sendTime) {
		var now = DateTime.UtcNow;
		var minTime = now.AddSeconds(300); // 5 minutes minimum
		var maxTime = now.AddDays(35); // 35 days maximum

		if (sendTime < minTime) {
			throw new ArgumentException("Scheduled send time must be at least 5 minutes in the future");
		}

		if (sendTime > maxTime) {
			throw new ArgumentException("Scheduled send time cannot be more than 35 days in the future");
		}
	}
	private static void ValidateStatusCallbackUrl(Uri statusCallbackUrl) {
		if (!statusCallbackUrl.IsAbsoluteUri) {
			throw new ArgumentException("Status callback URL must be an absolute URL");
		}
		if (statusCallbackUrl.Scheme != Uri.UriSchemeHttps) {
			throw new ArgumentException("Status callback URL must use HTTPS protocol");
		}
	}
	private static void ValidateMediaUrls(IEnumerable<Uri> mediaUrls) {
		if (mediaUrls.Count() > 10) {
			throw new ArgumentException("Maximum 10 media URLs allowed");
		}
	}
	private static void ValidateValidityPeriod(int totalSeconds) {
		if (totalSeconds < 10 || totalSeconds > 36000) {
			throw new ArgumentException("Validity period must be between 10 seconds and 10 hours (36,000 seconds)");
		}
	}
	private MessageResult ProcessMessageResult(MessageResource? result, string to, string logHeader) {

		if (result == null) {
			logger.LogNoResultReturned(logHeader);
			return new MessageResult(to, false, null, "No result returned from messaging service");
		}

		if (!SuccessStatuses.Contains(result.Status)) {
			var status = result.Status.ToString();
			var errMsg = result.ErrorMessage;
			logger.LogFailedWithStatus(logHeader, status, errMsg ?? "No error message");
			return new MessageResult(to, false, result.Sid, errMsg ?? $"Status: {result.Status}");
		}

		logger.LogSuccess(logHeader, result.Sid);
		return new MessageResult(to, true, result.Sid);

	}
	private string? ParsePhoneNumber(string phoneNumber, string countryCode) {
		try {
			var phone = this._phoneUtil.Parse(phoneNumber, countryCode);
			if (!this._phoneUtil.IsValidNumber(phone)) {
				logger.LogInvalidPhoneNumber(phoneNumber);
				return null;
			}

			return this._phoneUtil.Format(phone, PhoneNumbers.PhoneNumberFormat.E164);
		} catch (Exception ex) {
			logger.LogErrorParsingPhoneNumber(ex, phoneNumber);
			return null;
		}
	}

}