# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2026-02-02

### Added

- Initial release
- `Expo` client class for sending push notifications
- `SendPushNotificationsAsync` method for sending notifications
- `GetPushNotificationReceiptsAsync` method for retrieving delivery receipts
- `ChunkPushNotifications` method for batching messages (100 per chunk)
- `ChunkPushNotificationReceiptIds` method for batching receipt IDs (300 per chunk)
- `IsExpoPushToken` static method for token validation
- Automatic gzip compression for large payloads (>1KB)
- Automatic retry with exponential backoff for rate-limited requests
- Dependency injection support via `AddExpoClient` extension method
- Full support for all Expo push notification message properties
- Comprehensive XML documentation
- Support for .NET 8, 9, and 10
