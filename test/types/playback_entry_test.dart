import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/playback_entry.dart';

void main() {
  group('PlaybackEntry.fromSessionJson identity mapping', () {
    const baseUrl = 'https://example.com';

    Map<String, dynamic> basePayload({
      String? seriesName,
      String? name,
      String nowPlayingType = 'Episode',
      String? parentId = 'parent1',
      bool isPaused = false,
      int positionTicks = 300000000,
      int runTimeTicks = 600000000,
      Map<String, dynamic>? transcodingInfo,
    }) {
      final nowPlayingItem = <String, dynamic>{
        'Id': 'item1',
        'SeriesName': seriesName,
        'Name': name,
        'RunTimeTicks': runTimeTicks,
        'Type': nowPlayingType,
      };

      if (parentId != null) {
        nowPlayingItem['ParentId'] = parentId;
      }

      return {
        'UserName': 'User',
        'UserId': 'user1',
        'Client': 'Web',
        'DeviceName': 'Device',
        'NowPlayingItem': nowPlayingItem,
        'PlayState': {
          'PositionTicks': positionTicks,
          'IsPaused': isPaused,
        },
        if (transcodingInfo != null) 'TranscodingInfo': transcodingInfo,
      };
    }

    test('sets seriesName when present', () {
      final json = basePayload(seriesName: 'Series Title', name: 'Fallback Title');

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.identity.seriesName, 'Series Title');
      expect(entry.identity.title, 'Fallback Title');
    });

    test('leaves seriesName null when SeriesName is null', () {
      final json = basePayload(seriesName: null, name: 'Movie Title');

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.identity.seriesName, isNull);
      expect(entry.identity.title, 'Movie Title');
    });

    test('leaves seriesName null when SeriesName is missing', () {
      final json = basePayload(name: 'Standalone Title');
      (json['NowPlayingItem'] as Map<String, dynamic>).remove('SeriesName');

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.identity.seriesName, isNull);
      expect(entry.identity.title, 'Standalone Title');
    });

    test('uses parent item image for episodes when ParentId is present', () {
      final json = basePayload(
        nowPlayingType: 'Episode',
        parentId: 'parent123',
        name: 'Episode Name',
      );

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.contentKind, ContentKind.episode);
      expect(entry.identity.primaryImageUrl,
          'https://example.com/Items/parent123/Images/Primary');
    });

    test('uses item image for movies', () {
      final json = basePayload(
        nowPlayingType: 'Movie',
        parentId: null,
        name: 'Movie Name',
      );

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.contentKind, ContentKind.movie);
      expect(entry.identity.primaryImageUrl,
          'https://example.com/Items/item1/Images/Primary');
    });

    test('maps pause state from PlayState.IsPaused', () {
      final json = basePayload(isPaused: true, name: 'Title');

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.isPaused, isTrue);
    });

    test('computes endPercentage from position/runtime ticks', () {
      final json = basePayload(
        positionTicks: 300,
        runTimeTicks: 600,
        name: 'Title',
      );

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.timing.endPercentage, closeTo(50.0, 0.0001));
    });

    test('maps transcoding info when TranscodingInfo is present', () {
      final json = basePayload(
        name: 'Title',
        transcodingInfo: {
          'IsVideoDirect': false,
          'IsAudioDirect': true,
          'HardwareAccelerationType': 'vaapi',
          'Bitrate': 123456,
          'VideoCodec': 'h264',
          'AudioCodec': 'aac',
          'Container': 'mp4',
          'Height': 1080,
          'Width': 1920,
          'TranscodeReasons': ['VideoCodecNotSupported'],
          'CompletionPercentage': 12.5,
        },
      );

      final entry = PlaybackEntry.fromSessionJson(baseUrl, json);

      expect(entry.transcoding, isNotNull);
      expect(entry.transcoding!.isVideoDirect, isFalse);
      expect(entry.transcoding!.isAudioDirect, isTrue);
      expect(entry.transcoding!.hardwareAcceleration, 'vaapi');
      expect(entry.transcoding!.bitrate, 123456);
      expect(entry.transcoding!.reasons, contains('VideoCodecNotSupported'));
      expect(entry.transcoding!.completionPercentage, closeTo(12.5, 0.0001));
    });
  });
}
