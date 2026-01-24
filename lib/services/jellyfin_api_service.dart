import 'dart:convert';
import 'dart:async';
import 'package:http/http.dart' as http;
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/exceptions.dart';
import 'package:jellydash/types/activity_response.dart';
import 'package:jellydash/types/playback_entry.dart';

class JellyfinApiService implements ApiService {
  final String baseUrl;
  final String apiKey;

  JellyfinApiService({required this.baseUrl, required this.apiKey});

  Future<http.Response> _get(Uri url) async {
    try {
      return await http.get(url, headers: {
        'X-Emby-Token': apiKey,
      }).timeout(const Duration(seconds: 10));
    } on TimeoutException catch (_) {
      throw NetworkException(NetworkExceptionType.timeout);
    } on http.ClientException catch (_) {
      throw NetworkException(NetworkExceptionType.connection);
    }
  }

  @override
  Future<ActivityResponse> fetchActivity(
      bool includeActive, int? limit, String? cursor) async {
    final url = Uri.parse('$baseUrl/Sessions');
    final response = await _get(url);
    if (response.statusCode == 200) {
      var parsedResponse = jsonDecode(response.body) as List<dynamic>;
      var playbackEntries = parsedResponse
          .where((session) => session['NowPlayingItem'] != null)
          .map((session) => PlaybackEntry.fromSessionJson(baseUrl, session))
          .toList();
      playbackEntries
          .sort((a, b) => b.identity.title.compareTo(a.identity.title));
      return ActivityResponse(items: playbackEntries, nextCursor: null);
    } else if (response.statusCode == 404) {
      throw NotFoundException();
    } else if (response.statusCode == 401) {
      throw UnauthorizedException();
    } else {
      throw NetworkException(NetworkExceptionType.unknown);
    }
  }
}
