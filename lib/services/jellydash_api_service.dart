import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/exceptions.dart';
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
    } on TimeoutException catch (_) {
      throw NetworkException(NetworkExceptionType.timeout);
    } on http.ClientException catch (_) {
      throw NetworkException(NetworkExceptionType.connection);
    } catch (e) {
      throw NetworkException(NetworkExceptionType.unknown);
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
      throw NotFoundException();
    } else if (response.statusCode == 401) {
      throw UnauthorizedException();
    } else {
      throw Exception('Failed to load activity: ${response.statusCode}');
    }
  }
}
