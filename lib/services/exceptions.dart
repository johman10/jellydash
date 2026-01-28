class CustomException implements Exception {
  final bool fatal = false;
}

enum NetworkExceptionType { timeout, connection, unknown }

class NetworkException implements CustomException {
  final NetworkExceptionType type;

  @override
  final bool fatal = false;

  NetworkException(this.type);
}

class NotFoundException implements CustomException {
  @override
  final bool fatal = true;

  NotFoundException();
}

class UnauthorizedException implements CustomException {
  @override
  final bool fatal = true;

  UnauthorizedException();
}
