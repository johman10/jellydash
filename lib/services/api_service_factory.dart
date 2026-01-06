import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/jellydash_api_service.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';

class ApiServiceFactory {
  static ApiService create({
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
