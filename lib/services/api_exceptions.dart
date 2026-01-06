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
