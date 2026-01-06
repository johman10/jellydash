import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/api_exceptions.dart';
import 'package:jellydash/types/history_response.dart';
import 'package:jellydash/types/playback_entry.dart';

class JellyfinApiService implements ApiService {
  final String baseUrl;
  final String apiKey;

  JellyfinApiService({required this.baseUrl, required this.apiKey});

  Future<http.Response> _get(Uri url) async {
    try {
      return await http
          .get(url, headers: {
            'X-Emby-Token': apiKey,
          })
          .timeout(const Duration(seconds: 10));
    } on TimeoutException catch (e) {
      throw NetworkException(
        kind: NetworkFailureKind.timeout,
        message: 'Timed out connecting to the server.',
        cause: e,
      );
    } on http.ClientException catch (e) {
      throw NetworkException(
        kind: NetworkFailureKind.connection,
        message: 'Could not connect to the server. Check the server and network.',
        cause: e,
      );
    }
  }

  @override
  Future<List<PlaybackEntry>> fetchNowPlaying() async {
    final url = Uri.parse('$baseUrl/Sessions');
    final response = await _get(url);
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as List<dynamic>;
      var playbackEntries = parsedResponse
          .where((session) => session['NowPlayingItem'] != null)
          .map((session) => PlaybackEntry.fromSessionJson(baseUrl, session))
          .toList();
      playbackEntries.sort((a, b) => b.identity.title.compareTo(a.identity.title));
      return playbackEntries;
    } else if (response.statusCode == 404) {
      throw NotFoundException(
        'Endpoint not found (404). Check your base URL (and reverse proxy routing if used).',
        NotFoundService.jellyfin,
      );
    } else if (response.statusCode == 401) {
      throw UnauthorizedException(
        'Unauthorized (401). Check your API key.',
      );
    } else {
      throw Exception('Failed to load sessions: ${response.statusCode}');
    }
  }

  @override
  Future<HistoryResponse> fetchPlaybackHistory(String? cursor) {
    throw UnimplementedError();
  }
}
