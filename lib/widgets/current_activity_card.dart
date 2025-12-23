import 'package:flutter/material.dart';
import '../types/session.dart';

class CurrentActivityCard extends StatelessWidget {
  final Session session;
  const CurrentActivityCard({super.key, required this.session});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Text(
        '${session.userName ?? "Unknown"} is watching ${session.name} ${session.season}x${session.episode}.',
      ),
    );
  }
}
