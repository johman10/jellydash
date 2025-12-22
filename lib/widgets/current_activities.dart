import 'package:flutter/material.dart';
import '../types/session.dart';
import './current_activity_card.dart';

class CurrentActivities extends StatelessWidget {
  final bool isLoading;
  final List<Session> sessions;

  const CurrentActivities(
      {super.key, required this.isLoading, required this.sessions});

  Widget getCardContent(bool isLoading, List<Session> sessions) {
    if (isLoading) {
      return CircularProgressIndicator();
    }

    if (sessions.isEmpty) {
      return Text('No current activities.');
    }

    return Column(
      children: sessions!.map((session) {
        return CurrentActivityCard(session: session);
      }).toList(),
    );
  }

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: double.infinity,
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(24.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            spacing: 16,
            children: [
              const Text('Current Activities',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
              getCardContent(isLoading, sessions),
            ],
          ),
        ),
      ),
    );
  }
}
