import 'dart:convert';
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/exceptions.dart';
import 'package:jellydash/types/activity_response.dart';

class JellyDashApiService extends ApiService {
  JellyDashApiService({required super.baseUrl, required super.apiKey});

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
    final response = await get(url);
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as Map<String, dynamic>;
      return ActivityResponse.fromJson(baseUrl, apiKey, parsedResponse);
    } else if (response.statusCode == 404) {
      throw NotFoundException();
    } else if (response.statusCode == 401) {
      throw UnauthorizedException();
    } else {
      throw Exception('Failed to load activity: ${response.statusCode}');
    }
  }
}
