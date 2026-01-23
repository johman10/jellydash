import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'package:jellydash/widgets/now_playing.dart';
import 'package:jellydash/widgets/playback_entry_card.dart';

void main() {
  group('CurrentActivities layout', () {
    Widget wrapWithMaterial(Widget child) {
      return MaterialApp(
        home: Scaffold(
          body: Center(child: child),
        ),
      );
    }

    int ticksFromDuration(Duration duration) => duration.inMicroseconds * 10;

    PlaybackEntry buildEntry(String userName) {
      const duration = Duration(minutes: 10);
      const progress = Duration(minutes: 1);

      return PlaybackEntry(
        itemId: 'item1',
        parentItemId: null,
        contentType: ContentType.episode,
        identity: const ContentIdentity(
          title: 'Show',
          seasonNumber: 1,
          episodeNumber: 1,
          seriesName: 'Show',
        ),
        user: UserInfo(userId: 'user1', userName: userName),
        client: const ClientInfo(deviceName: 'Device', clientName: 'Web'),
        timing: TimingInfo(
          runtimeTicks: ticksFromDuration(duration),
          endPositionTicks: ticksFromDuration(progress),
        ),
        streams: const StreamInfo(),
        transcoding: const TranscodingInfo(
          isVideoDirect: true,
          isAudioDirect: true,
          reasons: [],
        ),
        isCompleted: false,
        isPaused: false,
      );
    }

    testWidgets('shows message when no sessions', (WidgetTester tester) async {
      await tester.pumpWidget(wrapWithMaterial(
        const NowPlaying(isLoading: false, nowPlayingEntries: []),
      ));

      expect(find.text('It\'s quiet... too quiet.'), findsOneWidget);
    });

    testWidgets('renders one column on narrow width',
        (WidgetTester tester) async {
      final sessions =
          List<PlaybackEntry>.generate(3, (i) => buildEntry('User$i'));

      // Use a tall surface so the column has enough vertical space
      await tester.binding.setSurfaceSize(const Size(320, 1200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapWithMaterial(
        NowPlaying(isLoading: false, nowPlayingEntries: sessions),
      ));

      await tester.pumpAndSettle();

      // All cards should be stacked vertically; we just assert they all exist.
      for (var i = 0; i < sessions.length; i++) {
        expect(find.textContaining('User$i'), findsOneWidget);
      }

      final cards = find.byType(PlaybackEntryCard);
      expect(cards, findsNWidgets(3));

      final p0 = tester.getTopLeft(cards.at(0));
      final p1 = tester.getTopLeft(cards.at(1));
      final p2 = tester.getTopLeft(cards.at(2));

      // In a single column layout, cards should share X and increase in Y.
      expect((p0.dx - p1.dx).abs(), lessThan(1.0));
      expect((p1.dx - p2.dx).abs(), lessThan(1.0));
      expect(p0.dy, lessThan(p1.dy));
      expect(p1.dy, lessThan(p2.dy));
    });

    testWidgets('renders two columns on medium width',
        (WidgetTester tester) async {
      final sessions =
          List<PlaybackEntry>.generate(3, (i) => buildEntry('User$i'));

      await tester.binding.setSurfaceSize(const Size(800, 1200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapWithMaterial(
        NowPlaying(isLoading: false, nowPlayingEntries: sessions),
      ));

      await tester.pumpAndSettle();

      final cards = find.byType(PlaybackEntryCard);
      expect(cards, findsNWidgets(3));

      final p0 = tester.getTopLeft(cards.at(0));
      final p1 = tester.getTopLeft(cards.at(1));
      final p2 = tester.getTopLeft(cards.at(2));

      // In a two column layout, first two share Y, third wraps to next row.
      expect((p0.dy - p1.dy).abs(), lessThan(1.0));
      expect(p2.dy, greaterThan(p0.dy + 1.0));
      expect(p0.dx, lessThan(p1.dx));
    });

    testWidgets('renders three columns on wide width',
        (WidgetTester tester) async {
      final sessions =
          List<PlaybackEntry>.generate(3, (i) => buildEntry('User$i'));

      await tester.binding.setSurfaceSize(const Size(1200, 1200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapWithMaterial(
        NowPlaying(isLoading: false, nowPlayingEntries: sessions),
      ));

      await tester.pumpAndSettle();

      final cards = find.byType(PlaybackEntryCard);
      expect(cards, findsNWidgets(3));

      final p0 = tester.getTopLeft(cards.at(0));
      final p1 = tester.getTopLeft(cards.at(1));
      final p2 = tester.getTopLeft(cards.at(2));

      // In a three column layout, all three should be on the same row.
      expect((p0.dy - p1.dy).abs(), lessThan(1.0));
      expect((p1.dy - p2.dy).abs(), lessThan(1.0));
      expect(p0.dx, lessThan(p1.dx));
      expect(p1.dx, lessThan(p2.dx));
    });

    testWidgets('renders multiple sessions without overflow',
        (WidgetTester tester) async {
      final sessions =
          List<PlaybackEntry>.generate(6, (i) => buildEntry('User$i'));

      await tester.binding.setSurfaceSize(const Size(1200, 800));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(wrapWithMaterial(
        NowPlaying(isLoading: false, nowPlayingEntries: sessions),
      ));

      await tester.pumpAndSettle();

      for (var i = 0; i < sessions.length; i++) {
        expect(find.textContaining('User$i'), findsOneWidget);
      }
    });
  });
}
