import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'package:jellydash/types/session.dart';
import 'package:mockito/mockito.dart';

// Simple mock classes for testing
class MockJellyfinApiService extends Mock implements JellyfinApiService {
  @override
  Future<List<Session>> fetchCurrentSessions() async {
    // Return a mock session list
    return [
      Session(
        userName: 'TestUser',
        client: 'Web',
        deviceName: 'TestDevice',
        name: 'Test Series',
        season: 1,
        episode: 2,
        year: 2022,
        imagePath: '/Items/12345/Images/Primary',
        video: SessionVideo(
          codec: 'h264',
          container: 'mp4',
          videoRange: 'SDR',
          bitRate: 800000,
          bitDepth: 8,
          height: 1080,
          width: 1920,
          isDirectStream: true,
        ),
        audio: SessionAudio(
          language: 'eng',
          codec: 'aac',
          layout: '5.1',
          bitRate: 192000,
          sampleRate: 48000,
          isDirectStream: true,
        ),
        subtitles: SessionSubtitle(
          isForced: false,
          isHearingImpaired: false,
          codec: 'srt',
          language: 'eng',
        ),
        transcodingInfo: TranscodingInfo(
          video: SessionVideo(
            codec: 'h264',
            container: 'mp4',
            isDirectStream: true,
          ),
          audio: SessionAudio(
            codec: 'aac',
            isDirectStream: true,
          ),
          reasons: ['Container not supported'],
          progress: 50.0,
          hardwareAcceleration: 'vaapi',
        ),
        progress: 75.0,
        isPlaying: true,
        isPaused: false,
        isMuted: false,
      )
    ];
  }
}

void main() {
  group('DashboardScreen', () {
    testWidgets('renders without error', (WidgetTester tester) async {
      final mockApiService = MockJellyfinApiService();
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      expect(find.byType(DashboardScreen), findsOneWidget);
    });

    testWidgets('shows loading indicator or activities',
        (WidgetTester tester) async {
      final mockApiService = MockJellyfinApiService();
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      // Should show a loading indicator or activities widget
      expect(
        find.byType(CircularProgressIndicator).evaluate().isNotEmpty ||
            find.byType(ListView).evaluate().isNotEmpty ||
            find.byType(Column).evaluate().isNotEmpty,
        true,
      );
    });
  });
}
