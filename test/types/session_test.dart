import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/session.dart';

void main() {
  group('Session.fromJson name mapping', () {
    const baseUrl = 'https://example.com';

    Map<String, dynamic> basePayload({
      String? seriesName,
      String? name,
    }) {
      return {
        'UserName': 'User',
        'Client': 'Web',
        'DeviceName': 'Device',
        'NowPlayingItem': {
          'Id': 'item1',
          'SeriesName': seriesName,
          'Name': name,
          'RunTimeTicks': 600000000,
        },
        'PlayState': {
          'PositionTicks': 300000000,
        },
      };
    }

    test('uses SeriesName when present', () {
      final json = basePayload(seriesName: 'Series Title', name: 'Fallback Title');

      final session = Session.fromJson(baseUrl, json);

      expect(session.name, 'Series Title');
    });

    test('falls back to Name when SeriesName is null', () {
      final json = basePayload(seriesName: null, name: 'Movie Title');

      final session = Session.fromJson(baseUrl, json);

      expect(session.name, 'Movie Title');
    });

    test('falls back to Name when SeriesName is missing', () {
      final json = basePayload(name: 'Standalone Title');
      (json['NowPlayingItem'] as Map<String, dynamic>).remove('SeriesName');

      final session = Session.fromJson(baseUrl, json);

      expect(session.name, 'Standalone Title');
    });
  });
}
