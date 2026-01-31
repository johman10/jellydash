import 'dart:async';

import 'package:http/http.dart' as http;
import 'package:jellydash/services/exceptions.dart';
import 'package:jellydash/services/jellydash_api_service.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'package:jellydash/types/activity_response.dart';
import 'package:meta/meta.dart';

abstract class ApiService {
  final String baseUrl;
  final String apiKey;

  ApiService({required this.baseUrl, required this.apiKey});

  Future<ActivityResponse> fetchActivity(
      bool includeActive, int? limit, String? cursor);

  @protected
  Future<http.Response> get(Uri url) async {
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

  factory ApiService.create({
    required String baseUrl,
    required String apiKey,
    required bool usePluginApi,
  }) {
    if (usePluginApi) {
      return JellyDashApiService(baseUrl: baseUrl, apiKey: apiKey);
    }

    return JellyfinApiService(baseUrl: baseUrl, apiKey: apiKey);
  }
}
