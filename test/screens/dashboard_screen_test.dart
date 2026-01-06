import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/screens/dashboard_screen.dart';
import 'package:jellydash/services/jellyfin_api_service.dart';
import 'package:jellydash/types/playback_entry.dart';
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

  int ticksFromDuration(Duration duration) => duration.inMicroseconds * 10;

  PlaybackEntry baseEntry({
    String? title,
    String? seriesName,
    int? season,
    int? episode,
    int? year,
    Duration? progress,
    Duration? duration,
    int? bitrate,
    bool isVideoDirect = true,
    bool isAudioDirect = true,
    String? userName,
    String? userImageUrl,
    String? imageUrl,
    bool isPaused = false,
  }) {
    final effectiveDuration = duration ?? const Duration(minutes: 10);
    final effectiveProgress = progress ?? const Duration(minutes: 1);

    return PlaybackEntry(
      itemId: 'item1',
      parentItemId: null,
      contentKind: ContentKind.other,
      identity: ContentIdentity(
        primaryImageUrl: imageUrl,
        title: title ?? 'Title',
        seriesName: seriesName,
        seasonNumber: season,
        episodeNumber: episode,
        year: year,
      ),
      user: UserInfo(
        userId: 'user1',
        userName: userName ?? 'User',
        userImageUrl: userImageUrl,
      ),
      client: const ClientInfo(deviceName: 'Device', clientName: 'Web'),
      timing: TimingInfo(
        runtimeTicks: ticksFromDuration(effectiveDuration),
        endPositionTicks: ticksFromDuration(effectiveProgress),
      ),
      streams: const StreamInfo(),
      transcoding: bitrate == null
          ? null
          : TranscodingInfo(
              isVideoDirect: isVideoDirect,
              isAudioDirect: isAudioDirect,
              bitrate: bitrate,
              reasons: const [],
            ),
      isCompleted: false,
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
      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'TestUser',
              seriesName: 'Test Series',
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
      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'TestUser',
              seriesName: 'Test Series',
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
      expect(find.byType(NowPlaying), findsOneWidget);
    });

    testWidgets('shows updated sessions when they change', (tester) async {
      const pollingInterval = 1;

      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'User1',
              seriesName: 'Show1',
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

      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'User2',
              seriesName: 'Show2',
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
      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'TestUser',
              seriesName: 'Test Series',
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
      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'TestUser',
              seriesName: 'Test Series',
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
      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => []);

      await tester.pumpWidget(wrapDashboard(mockApiService));
      await tester.pumpAndSettle();

      expect(find.text('It\'s quiet... too quiet.'), findsOneWidget);
    });

    testWidgets('shows multiple sessions', (tester) async {
      when(mockApiService.fetchNowPlaying()).thenAnswer((_) async => [
            baseEntry(
              userName: 'User1',
              seriesName: 'Show1',
              season: 1,
              episode: 1,
              progress: const Duration(minutes: 1),
              duration: const Duration(minutes: 10),
            ),
            baseEntry(
              userName: 'User2',
              seriesName: 'Show2',
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
