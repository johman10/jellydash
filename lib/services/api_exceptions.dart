enum NetworkFailureKind {
  /// Connection-level failure (refused, unreachable, etc.).
  connection,

  /// Request timed out.
  timeout,

  /// Unknown/unspecified network failure.
  unknown,
}

class NetworkException implements Exception {
  final NetworkFailureKind kind;
  final String message;
  final Object? cause;

  NetworkException({
    required this.kind,
    required this.message,
    this.cause,
  });

  @override
  String toString() => 'NetworkException(kind: $kind, message: $message)';
}

enum NotFoundService {
  jellydash,
  jellyfin,
}

class NotFoundException implements Exception {
  final String message;
  final NotFoundService service;

  NotFoundException(this.message, this.service);

  @override
  String toString() => 'NotFoundException: $message (Service: $service)';
}

class UnauthorizedException implements Exception {
  final String message;
  UnauthorizedException(this.message);

  @override
  String toString() => 'UnauthorizedException: $message';
}

class ApiErrorPresentation {
  final bool isFatal;
  final String title;
  final List<String> details;

  const ApiErrorPresentation({
    required this.isFatal,
    required this.title,
    required this.details,
  });

  String get shortMessage => title;
}

/// Maps known API exceptions to user-facing text and whether retrying polling
/// without changing settings is likely futile.
///
/// The [usePluginApi] flag is used to decide whether Jellydash-plugin 404s should
/// be treated as fatal.
ApiErrorPresentation presentApiError(
  Exception error, {
  required bool usePluginApi,
}) {
  if (error is NotFoundException) {
    if (error.service == NotFoundService.jellydash) {
      return ApiErrorPresentation(
        isFatal: usePluginApi,
        title: 'Houston, we have a problem',
        details: [
          'The Jellydash plugin seems to be taking a vacation. Install it on your Jellyfin server and give Jellyfin a quick restart.',
          'Not feeling the plugin life? Disable "Use Jellydash Plugin API" in Settings.',
        ],
      );
    }

    return const ApiErrorPresentation(
      isFatal: true,
      title: 'This is not the server you\'re looking for',
      details: [
        'Double-check the API Hostname in Settings. If you\'re using a reverse proxy, make sure the routing is on point.',
      ],
    );
  }

  if (error is UnauthorizedException) {
    return const ApiErrorPresentation(
      isFatal: true,
      title: 'You shall not pass!',
      details: [
        'Looks like your API key isn\'t cutting it. Head to Settings and make sure it\'s correct.',
      ],
    );
  }

  if (error is NetworkException) {
    switch (error.kind) {
      case NetworkFailureKind.timeout:
        return const ApiErrorPresentation(
          isFatal: false,
          title: 'Still waiting... and waiting...',
          details: [
            'Your server is taking its sweet time. Check if it\'s awake and your network connection is stable, then try again.',
          ],
        );
      case NetworkFailureKind.connection:
        return const ApiErrorPresentation(
          isFatal: false,
          title: 'Can you hear me now?',
          details: [
            'We can\'t reach your server. Make sure it\'s running and your network isn\'t playing hide and seek.',
          ],
        );
      case NetworkFailureKind.unknown:
        return const ApiErrorPresentation(
          isFatal: false,
          title: 'Something went sideways',
          details: [
            'We hit a network snag. Check your server status and connection, then give it another shot.',
          ],
        );
    }
  }

  return const ApiErrorPresentation(
    isFatal: false,
    title: 'Well, that was unexpected',
    details: [
      'Something quirky happened. Try again, and if it keeps acting up, peek at your server logs for clues.',
    ],
  );
}
