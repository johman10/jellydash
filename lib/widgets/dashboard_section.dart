import 'package:flutter/material.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/theme/jellydash_theme.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'playback_entry_card.dart';

class DashboardSection extends StatelessWidget {
  final bool isLoading;
  final List<PlaybackEntry> entries;
  final Exception? error;
  final String title;
  final String emptyMessage;

  const DashboardSection(
      {super.key,
      required this.isLoading,
      required this.entries,
      required this.title,
      required this.emptyMessage,
      this.error});

  Widget getContent(bool isLoading, List<PlaybackEntry> entries, String emptyMessage) {
    if (isLoading) {
      return const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Padding(
          padding: EdgeInsets.all(24),
          child: CircularProgressIndicator(),
        ),
      ]);
    }

    if (entries.isEmpty) {
      return Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Padding(
          padding: EdgeInsets.all(24),
          child: Text(emptyMessage),
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
          children: entries.map((entry) {
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
          Text(
            title,
            style: JellydashTextStyles.sectionTitle,
          ),
          getContent(isLoading, entries, emptyMessage),
        ],
      ),
    );
  }
}
