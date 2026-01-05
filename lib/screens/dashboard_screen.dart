import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/types/session.dart';
import 'dart:async';
import '../services/jellyfin_api_service.dart';
import '../widgets/now_playing.dart';
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

  Future<void> _fetchSessions() {
    // TODO: Error handling.
    // In case of an error add the appropriate state.
    // Then show instructions to verify the configuration with a button to settings.
    return widget.apiService.fetchCurrentSessions().then((data) {
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
    // Enable pull-to-refresh only on mobile platforms (Android/iOS)
    final platform = Theme.of(context).platform;
    final isTouchDevice =
        platform == TargetPlatform.android || platform == TargetPlatform.iOS;
    return AppScaffold(
      title: 'Jellydash',
      actions: [
        IconButton(
          icon: const Icon(Icons.settings),
          tooltip: 'Settings',
          onPressed: () {
            GoRouter.of(context).push('/settings');
          },
        ),
      ],
      onRefresh: isTouchDevice ? _fetchSessions : null,
      children: [
        SizedBox(
          width: double.infinity,
          child: Column(spacing: 16, children: [
            CurrentActivities(isLoading: _initialLoading, sessions: _sessions),
            const RecentActivityCard(),
          ]),
        )
      ],
    );
  }
}
