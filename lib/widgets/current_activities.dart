import 'package:flutter/material.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/theme/jellydash_theme.dart';
import '../types/session.dart';
import './current_activity_card.dart';

class CurrentActivities extends StatelessWidget {
  final bool isLoading;
  final List<Session> sessions;

  const CurrentActivities(
      {super.key, required this.isLoading, required this.sessions});

  Widget getContent(bool isLoading, List<Session> sessions) {
    if (isLoading) {
      return const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Padding(
          padding: EdgeInsets.all(24),
          child: CircularProgressIndicator(),
        ),
      ]);
    }

    if (sessions.isEmpty) {
      return const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
        Padding(
          padding: EdgeInsets.all(24),
          child: Text('No current activities.'),
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
          children: sessions.map((session) {
            return SizedBox(
              width: cardWidth,
              child: CurrentActivityCard(session: session),
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
            'Current Activities',
            style: JellydashTextStyles.sectionTitle,
          ),
          getContent(isLoading, sessions),
        ],
      ),
    );
  }
}
