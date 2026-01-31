import 'dart:convert';
import 'package:jellydash/services/api_service.dart';
import 'package:jellydash/services/exceptions.dart';
import 'package:jellydash/types/activity_response.dart';
import 'package:jellydash/types/playback_entry.dart';

class JellyfinApiService extends ApiService {
  JellyfinApiService({required super.baseUrl, required super.apiKey});

  @override
  Future<ActivityResponse> fetchActivity(
      bool includeActive, int? limit, String? cursor) async {
    final url = Uri.parse('$baseUrl/Sessions');
    final response = await get(url);
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
