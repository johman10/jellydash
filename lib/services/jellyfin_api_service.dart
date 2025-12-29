import 'dart:convert';
import 'package:http/http.dart' as http;
import '../types/session.dart';

class JellyfinApiService {
  final String baseUrl;
  final String apiKey;

  JellyfinApiService({required this.baseUrl, required this.apiKey});

  Future<List<Session>> fetchCurrentSessions() async {
    final url = Uri.parse('$baseUrl/Sessions');
    final response = await http.get(url, headers: {
      'X-Emby-Token': apiKey,
    });
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as List<dynamic>;
      return parsedResponse
          .map((sessionJson) => Session.fromJson(baseUrl, sessionJson))
          .where((session) => session.isPlaying)
          .toList();
    } else {
      throw Exception('Failed to load sessions: ${response.statusCode}');
    }
  }
}
