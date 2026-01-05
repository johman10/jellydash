import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'package:jellydash/types/session.dart';
import 'package:jellydash/widgets/now_playing.dart';
import 'package:mockito/annotations.dart';
import 'package:mockito/mockito.dart';
import 'package:go_router/go_router.dart';

import 'dashboard_screen_test.mocks.dart';

@GenerateMocks([JellyfinApiService])
void main() {
  Widget wrapDashboard(MockJellyfinApiService apiService,
      {int pollingInterval = 1}) {
    final router = GoRouter(
      routes: [
        GoRoute(
          path: '/',
          builder: (context, state) => DashboardScreen(
            apiService: apiService,
            pollingInterval: pollingInterval,
          ),
        ),
      ],
      initialLocation: '/',
    );
    return MaterialApp.router(
      routerDelegate: router.routerDelegate,
      routeInformationParser: router.routeInformationParser,
      routeInformationProvider: router.routeInformationProvider,
    );
  }

  Session baseSession({
    String? name,
    int? season,
    int? episode,
    int? year,
    Duration? progress,
    Duration? duration,
    int? bitrate,
    TranscodingInfo? transcodingInfo,
    String? userName,
    String? userImageUrl,
    String? imageUrl,
    bool isPaused = false,
  }) {
    return Session(
      userName: userName ?? 'User',
      client: 'Web',
      deviceName: 'Device',
      name: name ?? 'Title',
      season: season,
      episode: episode,
      year: year,
      imageUrl: imageUrl ?? '',
      video: SessionVideo(),
      audio: SessionAudio(),
      subtitles: SessionSubtitle(),
      transcodingInfo: transcodingInfo ??
          TranscodingInfo(video: SessionVideo(), audio: SessionAudio(), reasons: []),
      progress: progress ?? const Duration(minutes: 1),
      duration: duration ?? const Duration(minutes: 10),
      bitrate: bitrate,
      userImageUrl: userImageUrl,
      isPaused: isPaused,
    );
  }

  group('DashboardScreen', () {
    late MockJellyfinApiService mockApiService;

    setUp(() {
      mockApiService = MockJellyfinApiService();
    });

    tearDown(() {
      reset(mockApiService);
    });

    testWidgets('renders without error', (tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'TestUser',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imageUrl: '/Items/12345/Images/Primary',
              progress: const Duration(minutes: 45),
              duration: const Duration(minutes: 60),
            ),
          ]);

      await tester.pumpWidget(wrapDashboard(mockApiService));

      expect(find.byType(DashboardScreen), findsOneWidget);
    });

    testWidgets('shows loading indicator and activities', (tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'TestUser',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imageUrl: '/Items/12345/Images/Primary',
              progress: const Duration(minutes: 45),
              duration: const Duration(minutes: 60),
            ),
          ]);

      await tester.pumpWidget(wrapDashboard(mockApiService));

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      await tester.pump();
      expect(find.byType(CurrentActivities), findsOneWidget);
    });

    testWidgets('shows updated sessions when they change', (tester) async {
      const pollingInterval = 1;

      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'User1',
              name: 'Show1',
              season: 1,
              episode: 1,
              progress: const Duration(minutes: 1),
              duration: const Duration(minutes: 10),
            ),
          ]);

      await tester.pumpWidget(
        wrapDashboard(
          mockApiService,
          pollingInterval: pollingInterval,
        ),
      );

      await tester.pumpAndSettle();
      expect(find.textContaining('User1'), findsOneWidget);

      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'User2',
              name: 'Show2',
              season: 2,
              episode: 2,
              progress: const Duration(minutes: 9),
              duration: const Duration(minutes: 10),
            ),
          ]);

      await tester.pump(const Duration(seconds: pollingInterval + 1));
      await tester.pumpAndSettle();

      expect(find.textContaining('User2'), findsOneWidget);
      expect(find.textContaining('User1'), findsNothing);
    });

    testWidgets('shows RecentActivities', (tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'TestUser',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imageUrl: '/Items/12345/Images/Primary',
              progress: const Duration(minutes: 45),
              duration: const Duration(minutes: 60),
            ),
          ]);

      await tester.pumpWidget(wrapDashboard(mockApiService));

      expect(find.text('Recent Activities'), findsOneWidget);
    });

    testWidgets('shows CurrentActivities with mock session', (tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'TestUser',
              name: 'Test Series',
              season: 1,
              episode: 2,
              year: 2022,
              imageUrl: '/Items/12345/Images/Primary',
              progress: const Duration(minutes: 45),
              duration: const Duration(minutes: 60),
            ),
          ]);

      await tester.pumpWidget(wrapDashboard(mockApiService));
      await tester.pumpAndSettle();

      expect(find.textContaining('TestUser'), findsOneWidget);
      expect(find.textContaining('Test Series'), findsOneWidget);
    });

    testWidgets('shows no activities when session list is empty', (tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => []);

      await tester.pumpWidget(wrapDashboard(mockApiService));
      await tester.pumpAndSettle();

      expect(find.text('It\'s quiet... too quiet.'), findsOneWidget);
    });

    testWidgets('shows multiple sessions', (tester) async {
      when(mockApiService.fetchCurrentSessions()).thenAnswer((_) async => [
            baseSession(
              userName: 'User1',
              name: 'Show1',
              season: 1,
              episode: 1,
              progress: const Duration(minutes: 1),
              duration: const Duration(minutes: 10),
            ),
            baseSession(
              userName: 'User2',
              name: 'Show2',
              season: 2,
              episode: 2,
              progress: const Duration(minutes: 9),
              duration: const Duration(minutes: 10),
            ),
          ]);

      await tester.pumpWidget(wrapDashboard(mockApiService));
      await tester.pumpAndSettle();

      expect(find.textContaining('User1'), findsOneWidget);
      expect(find.textContaining('User2'), findsOneWidget);
    });
  });
}
