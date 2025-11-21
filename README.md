# Cirreum.Communications.Sms.Twilio

[![NuGet Version](https://img.shields.io/nuget/v/Cirreum.Communications.Sms.Twilio.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Communications.Sms.Twilio/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Cirreum.Communications.Sms.Twilio.svg?style=flat-square&labelColor=1F1F1F&color=003D8F)](https://www.nuget.org/packages/Cirreum.Communications.Sms.Twilio/)
[![GitHub Release](https://img.shields.io/github/v/release/cirreum/Cirreum.Communications.Sms.Twilio?style=flat-square&labelColor=1F1F1F&color=FF3B2E)](https://github.com/cirreum/Cirreum.Communications.Sms.Twilio/releases)
[![License](https://img.shields.io/github/license/cirreum/Cirreum.Communications.Sms.Twilio?style=flat-square&labelColor=1F1F1F&color=F2F2F2)](https://github.com/cirreum/Cirreum.Communications.Sms.Twilio/blob/main/LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-003D8F?style=flat-square&labelColor=1F1F1F)](https://dotnet.microsoft.com/)

**A robust, production-ready SMS library for Twilio integration in .NET applications.**

## Overview

**Cirreum.Communications.Sms.Twilio** provides a comprehensive, enterprise-grade SMS communication solution built on the Twilio platform. It offers seamless integration with .NET applications through a clean, type-safe API with built-in health checks, bulk messaging capabilities, and advanced features like retry logic and phone number validation.

## Features

- **🚀 Simple API** - Clean, intuitive methods for sending SMS messages
- **📱 Phone Number Validation** - Built-in libphonenumber integration for reliable validation
- **⚡ Bulk Messaging** - Efficient parallel processing for large message batches
- **🔄 Retry Logic** - Exponential backoff with jittered delays for rate limiting
- **🏥 Health Checks** - Comprehensive health monitoring with configurable validation
- **⚙️ Flexible Configuration** - Support for Twilio messaging services or direct phone numbers
- **🎯 Advanced Options** - Scheduled sending, media attachments, status callbacks, validity periods
- **🔧 DI Integration** - First-class dependency injection support with keyed services
- **📊 Structured Logging** - Rich logging with proper correlation and error details

## Quick Start

### Installation

```bash
dotnet add package Cirreum.Communications.Sms.Twilio
```

### Basic Usage

```csharp
// Register with DI container
builder.AddTwilioSmsClient("default", settings => {
    settings.AccountSid = "your-account-sid";
    settings.AuthToken = "your-auth-token";
    settings.From = "+1234567890"; // or use ServiceId
});

// Inject and use
public class NotificationService {
    private readonly ISmsService _sms;
    
    public NotificationService(ISmsService sms) => _sms = sms;
    
    public async Task SendWelcomeMessage(string phoneNumber) {
        var result = await _sms.SendFromAsync(
            from: "+1234567890",
            to: phoneNumber,
            message: "Welcome to our service!");
            
        if (result.Success) {
            // Message sent successfully
            Console.WriteLine($"Message ID: {result.MessageId}");
        }
    }
}
```

### Bulk Messaging

```csharp
var phoneNumbers = new[] { "+1234567890", "+0987654321", "+1122334455" };

var response = await _sms.SendBulkAsync(
    message: "System maintenance scheduled for tonight.",
    phoneNumbers: phoneNumbers,
    serviceId: "your-messaging-service-sid");

Console.WriteLine($"Sent: {response.Sent}, Failed: {response.Failed}");
```

### Advanced Options

```csharp
var options = new SmsOptions {
    ScheduledSendTime = DateTime.UtcNow.AddHours(2),
    MediaUrls = [new Uri("https://example.com/image.png")],
    StatusCallbackUrl = new Uri("https://your-app.com/sms/status"),
    ValidityPeriod = TimeSpan.FromHours(4)
};

await _sms.SendViaServiceAsync(
    serviceId: "your-service-id",
    to: "+1234567890",
    message: "Check out this image!",
    options: options);
```

## Configuration

### Via appsettings.json

```json
{
  "ServiceProviders": {
    "Communications": {
      "Twilio": {
        "Instances": {
          "default": {
            "AccountSid": "your-account-sid",
            "AuthToken": "your-auth-token",
            "ServiceId": "your-messaging-service-sid",
            "Region": "us1",
            "Edge": "umatilla",
            "MaxRetries": 3,
            "BulkOptions": {
              "MaxConcurrency": 10
            }
          }
        }
      }
    }
  }
}
```

### Via Connection String (Key Vault)

```csharp
builder.AddTwilioSmsClient("production", 
    connectionString: """{"AccountSid":"ACxxx","AuthToken":"xxx","ServiceId":"MGxxx"}""");
```

## Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddCheck<TwilioSmsHealthCheck>("twilio-sms");

// Configure health check options
builder.AddTwilioSmsClient("default", settings, healthOptions => {
    healthOptions.TestSending = false; // Set to true for production validation
    healthOptions.PhoneNumber = "+1234567890"; // Test phone number
    healthOptions.CachedResultTimeout = TimeSpan.FromHours(6);
});
```

## Contribution Guidelines

1. **Be conservative with new abstractions**  
   The API surface must remain stable and meaningful.

2. **Limit dependency expansion**  
   Only add foundational, version-stable dependencies.

3. **Favor additive, non-breaking changes**  
   Breaking changes ripple through the entire ecosystem.

4. **Include thorough unit tests**  
   All primitives and patterns should be independently testable.

5. **Document architectural decisions**  
   Context and reasoning should be clear for future maintainers.

6. **Follow .NET conventions**  
   Use established patterns from Microsoft.Extensions.* libraries.

## Versioning

Cirreum.Communications.Sms.Twilio follows [Semantic Versioning](https://semver.org/):

- **Major** - Breaking API changes
- **Minor** - New features, backward compatible
- **Patch** - Bug fixes, backward compatible

Given its foundational role, major version bumps are rare and carefully considered.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

**Cirreum Foundation Framework**  
*Layered simplicity for modern .NET*