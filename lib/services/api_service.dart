import 'dart:async';

import 'package:jellydash/types/history_response.dart';
import 'package:jellydash/types/playback_entry.dart';

abstract class ApiService {
  Future<List<PlaybackEntry>> fetchNowPlaying();

  Future<HistoryResponse> fetchPlaybackHistory(String? cursor);
}
