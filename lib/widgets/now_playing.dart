import 'package:flutter/material.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/theme/jellydash_theme.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'package:jellydash/widgets/error_message.dart';
import 'playback_entry_card.dart';

class NowPlaying extends StatelessWidget {
  final bool isLoading;
  final List<PlaybackEntry> nowPlayingEntries;
  final Exception? error;

  const NowPlaying(
      {super.key,
      required this.isLoading,
      required this.nowPlayingEntries,
      this.error});

  Widget getContent(bool isLoading, List<PlaybackEntry> nowPlayingEntries) {
    if (isLoading) {
      return const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Padding(
          padding: EdgeInsets.all(24),
          child: CircularProgressIndicator(),
        ),
      ]);
    }

    if (error != null) {
      return ErrorMessage(error: error!);
    }

    if (nowPlayingEntries.isEmpty) {
      return const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Padding(
          padding: EdgeInsets.all(24),
          child: Text('It\'s quiet... too quiet.'),
        ),
      ]);
    }

    const spacing = 16.0;
    const minCardWidth = (AppScaffold.maxContentWidth / 3) - (spacing * 2);

    return LayoutBuilder(
      builder: (context, constraints) {
        final availableWidth = constraints.maxWidth
            .clamp(0, AppScaffold.maxContentWidth)
            .toDouble();

        // Decide how many columns we can fit based on a minimum card width.
        int columns;
        if (availableWidth >= minCardWidth * 3) {
          columns = 3;
        } else if (availableWidth >= minCardWidth * 2) {
          columns = 2;
        } else {
          columns = 1;
        }

        // Compute the width each card should take so that:
        //   columns * cardWidth + (columns - 1) * spacing == availableWidth
        final totalSpacing = spacing * (columns - 1);
        final cardWidth = (availableWidth - totalSpacing) / columns;

        return Wrap(
          spacing: spacing, // space between columns only
          // runSpacing: spacing,
          children: nowPlayingEntries.map((entry) {
            return SizedBox(
              width: cardWidth,
              child: PlaybackEntryCard(entry: entry),
            );
          }).toList(),
        );
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        spacing: 8,
        children: [
          const Text(
            'Now Playing',
            style: JellydashTextStyles.sectionTitle,
          ),
          getContent(isLoading, nowPlayingEntries),
        ],
      ),
    );
  }
}
