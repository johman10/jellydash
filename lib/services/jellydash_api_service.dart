import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/api_exceptions.dart';
import 'package:jellydash/types/history_response.dart';
import 'package:jellydash/types/playback_entry.dart';

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
  Future<List<PlaybackEntry>> fetchNowPlaying() async {
    final url = Uri.parse('$baseUrl/Jellydash/now-playing');
    final response = await _get(url);
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as List<dynamic>;
      return parsedResponse
          .map((nowPlayingJson) => PlaybackEntry.fromJson(baseUrl, nowPlayingJson))
          .toList();
    } else if (response.statusCode == 404) {
      throw NotFoundException(
        'Endpoint not found (404). Check your base URL and that the Jellydash plugin is installed.',
        NotFoundService.jellydash,
      );
    } else {
      throw Exception('Failed to load now playing: ${response.statusCode}');
    }
  }

  @override
  Future<HistoryResponse> fetchPlaybackHistory(String? cursor) async {
    final url = Uri.parse('$baseUrl/Jellydash/history').replace(queryParameters: {
      if (cursor != null) 'cursor': cursor,
    });
    final response = await _get(url);
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as Map<String, dynamic>;
      return HistoryResponse.fromJson(baseUrl, parsedResponse);
    } else if (response.statusCode == 404) {
      throw NotFoundException(
        'Endpoint not found (404). Check your base URL and that the Jellydash plugin is installed.',
        NotFoundService.jellydash,
      );
    } else {
      throw Exception('Failed to load history: ${response.statusCode}');
    }
  }
}
