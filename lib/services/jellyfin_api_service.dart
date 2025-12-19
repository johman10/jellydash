import 'dart:convert';
import 'package:http/http.dart' as http;

// Jellyfin API service for fetching activity data
// Example endpoint: GET /Sessions (https://jellyfin.org/docs/general/api/rest-api.html#tag/Sessions)
class JellyfinApiService {
  final String baseUrl;
  final String apiKey;

  JellyfinApiService({required this.baseUrl, required this.apiKey});

  Future<List<dynamic>> fetchCurrentSessions() async {
    final url = Uri.parse('$baseUrl/Sessions');
    final response = await http.get(url, headers: {
      'X-Emby-Token': apiKey,
    });
    if (response.statusCode == 200) {
      return jsonDecode(response.body) as List<dynamic>;
    } else {
      throw Exception('Failed to load sessions: ${response.statusCode}');
    }
  }
}
