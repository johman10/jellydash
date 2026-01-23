import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/api_exceptions.dart';
import 'package:jellydash/types/activity_response.dart';

class JellyDashApiService implements ApiService {
  final String baseUrl;
  final String apiKey;

  JellyDashApiService({required this.baseUrl, required this.apiKey});

  Future<http.Response> _get(Uri url) async {
    try {
      return await http.get(url, headers: {
        'X-Emby-Token': apiKey,
      }).timeout(const Duration(seconds: 10));
    } on TimeoutException catch (e) {
      throw NetworkException(
        kind: NetworkFailureKind.timeout,
        message: 'Timed out connecting to the server.',
        cause: e,
      );
    } on http.ClientException catch (e) {
      throw NetworkException(
        kind: NetworkFailureKind.connection,
        message:
            'Could not connect to the server. Check the server and network.',
        cause: e,
      );
    }
  }

  @override
  Future<ActivityResponse> fetchActivity(
    bool includeActive,
    int? limit,
    String? cursor,
  ) async {
    final url =
        Uri.parse('$baseUrl/Jellydash/activity').replace(queryParameters: {
      if (cursor != null) 'cursor': cursor,
      if (limit != null) 'limit': limit.toString(),
      'includeActive': includeActive.toString(),
    });
    final response = await _get(url);
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as Map<String, dynamic>;
      return ActivityResponse.fromJson(baseUrl, parsedResponse);
    } else if (response.statusCode == 404) {
      throw NotFoundException(
        'Endpoint not found (404). Check your base URL and that the Jellydash plugin is installed.',
        NotFoundService.jellydash,
      );
    } else {
      throw Exception('Failed to load activity: ${response.statusCode}');
    }
  }
}
