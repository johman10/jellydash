import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'package:jellydash/types/session.dart';
import 'package:jellydash/widgets/current_activities.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';

import 'dashboard_screen_test.mocks.dart';

@GenerateMocks([JellyfinApiService])
void main() {
  group('DashboardScreen', () {
    late MockJellyfinApiService mockApiService;

    setUp(() {
      mockApiService = MockJellyfinApiService();
    });

    tearDown(() {
      reset(mockApiService);
    });

    testWidgets('renders without error', (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'TestUser',
              client: 'Web',
              deviceName: 'TestDevice',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imagePath: '/Items/12345/Images/Primary',
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 75.0,
              isPlaying: true,
              isPaused: false,
              isMuted: false,
            )
          ]);
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

    testWidgets('shows loading indicator and activities',
        (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'TestUser',
              client: 'Web',
              deviceName: 'TestDevice',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imagePath: '/Items/12345/Images/Primary',
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 75.0,
              isPlaying: true,
              isPaused: false,
              isMuted: false,
            )
          ]);
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      // Should show a loading indicator initially
      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      await tester.pump();
      expect(find.byType(CurrentActivities), findsOneWidget);
    });

    testWidgets('shows updated sessions when they change', (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'User1',
              name: 'Show1',
              season: 1,
              episode: 1,
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 0.1,
            )
          ]);
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.textContaining('User1'), findsOneWidget);
      // Update the mock to return a different session
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'User2',
              name: 'Show2',
              season: 2,
              episode: 2,
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 0.9,
            )
          ]);
      // Wait for the polling interval to trigger an update
      await tester.pump(Duration(seconds: pollingInterval + 1));
      await tester.pumpAndSettle();
      expect(find.textContaining('User2'), findsOneWidget);
      expect(find.textContaining('User1'), findsNothing);
    });

    testWidgets('shows RecentActivityCard', (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'TestUser',
              client: 'Web',
              deviceName: 'TestDevice',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imagePath: '/Items/12345/Images/Primary',
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 75.0,
              isPlaying: true,
              isPaused: false,
              isMuted: false,
            )
          ]);
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      expect(find.text('Recent Activity'), findsOneWidget);
    });

    testWidgets('shows CurrentActivities with mock session',
        (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'TestUser',
              client: 'Web',
              deviceName: 'TestDevice',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imagePath: '/Items/12345/Images/Primary',
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 75.0,
              isPlaying: true,
              isPaused: false,
              isMuted: false,
            )
          ]);
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.textContaining('TestUser'), findsOneWidget);
      expect(find.textContaining('Test Series'), findsOneWidget);
    });

    testWidgets('shows no activities when session list is empty',
        (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => []);
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.text('No current activities.'), findsOneWidget);
    });

    testWidgets('shows multiple sessions', (WidgetTester tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            Session(
              userName: 'User1',
              name: 'Show1',
              season: 1,
              episode: 1,
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 0.1,
            ),
            Session(
              userName: 'User2',
              name: 'Show2',
              season: 2,
              episode: 2,
              video: SessionVideo(),
              audio: SessionAudio(),
              subtitles: SessionSubtitle(),
              transcodingInfo: TranscodingInfo(
                  video: SessionVideo(), audio: SessionAudio(), reasons: []),
              progress: 0.9,
            ),
          ]);
      const pollingInterval = 1;
      await tester.pumpWidget(
        MaterialApp(
          home: DashboardScreen(
            apiService: mockApiService,
            pollingInterval: pollingInterval,
          ),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.textContaining('User1'), findsOneWidget);
      expect(find.textContaining('User2'), findsOneWidget);
    });
  });
}
