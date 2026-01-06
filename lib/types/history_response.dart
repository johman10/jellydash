import 'package:jellydash/types/playback_entry.dart';

class HistoryResponse {
  final List<PlaybackEntry> items;
  final String? nextCursor;

  HistoryResponse({required this.items, this.nextCursor});

  factory HistoryResponse.fromJson(String baseUrl, Map<String, dynamic> json) {
    return HistoryResponse(
      items: (json['items'] as List<dynamic>)
          .map((itemJson) => PlaybackEntry.fromJson(baseUrl, itemJson))
          .toList(),
      nextCursor: json['next_cursor'],
    );
  }
}
