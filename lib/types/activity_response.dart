import 'package:jellydash/types/playback_entry.dart';

class ActivityResponse {
  final List<PlaybackEntry> items;
  final String? nextCursor;

  ActivityResponse({required this.items, this.nextCursor});

  factory ActivityResponse.fromJson(String baseUrl, Map<String, dynamic> json) {
    return ActivityResponse(
      items: (json['items'] as List<dynamic>)
          .map((itemJson) => PlaybackEntry.fromJson(baseUrl, itemJson))
          .toList(),
      nextCursor: json['next_cursor'],
    );
  }
}
