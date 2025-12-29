import 'package:flutter/material.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/types/session.dart';
import 'dart:async';
import '../services/jellyfin_api_service.dart';
import '../widgets/current_activities.dart';
import '../widgets/recent_activities.dart';

class DashboardScreen extends StatefulWidget {
  final JellyfinApiService apiService;
  final int pollingInterval;
  const DashboardScreen({
    super.key,
    required this.apiService,
    required this.pollingInterval,
  });

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  List<Session> _sessions = [];
  bool _initialLoading = true;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _startPolling();
  }

  Future<void> _startPolling() async {
    _pollingTimer?.cancel();

    setState(() {
      _initialLoading = true;
    });

    _fetchSessions();
    _pollingTimer =
        Timer.periodic(Duration(seconds: widget.pollingInterval), (timer) {
      _fetchSessions();
    });
  }

  void _fetchSessions() {
    // TODO: Error handling.
    // In case of an error add the appropriate state.
    // Then show instructions to verify the configuration with a button to settings.
    widget.apiService.fetchCurrentSessions().then((data) {
      data.sort((a, b) {
        final aDateCreated = a.dateCreated;
        final bDateCreated = b.dateCreated;
        if (aDateCreated == null && bDateCreated == null) {
          return 0;
        }
        if (aDateCreated == null) {
          // Sessions without a creation date are shown after those with one.
          return 1;
        }
        if (bDateCreated == null) {
          return -1;
        }
        return bDateCreated.compareTo(aDateCreated);
      });
      setState(() {
        _sessions = data;
        _initialLoading = false;
      });
    });
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      children: [
        SizedBox(
          width: double.infinity,
          child: Column(spacing: 16, children: [
            CurrentActivities(isLoading: _initialLoading, sessions: _sessions),
            const RecentActivityCard(),
          ]),
        ),
      ],
    );
  }
}
