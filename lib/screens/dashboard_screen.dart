import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'dart:async';
import '../services/api_service.dart';
import '../widgets/now_playing.dart';
import '../widgets/recent_activities.dart';

class DashboardScreen extends StatefulWidget {
  final ApiService apiService;
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
  List<PlaybackEntry> _nowPlayingEntries = [];
  bool _initialLoading = true;
  Exception? _error;
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

    _fetchNowPlaying();
    _pollingTimer =
        Timer.periodic(Duration(seconds: widget.pollingInterval), (timer) {
      _fetchNowPlaying();
    });
  }

  Future<void> _fetchNowPlaying() {
    return widget.apiService.fetchNowPlaying().then((data) {
      setState(() {
        _nowPlayingEntries = data;
        _initialLoading = false;
      });
    }).catchError((error) {
      // TODO: Find a way to restart the pollingTimer when settings have been updated
      // if ([
      //   'NotFoundException',
      //   'UnauthorizedException',
      // ].contains(error.runtimeType.toString())) {
      //   // Stop polling on critical errors
      //   _pollingTimer?.cancel();
      // }

      setState(() {
        _error = error as Exception?;
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
      onRefresh: isTouchDevice ? _fetchNowPlaying : null,
      children: [
        SizedBox(
          width: double.infinity,
          child: Column(spacing: 16, children: [
            NowPlaying(isLoading: _initialLoading, nowPlayingEntries: _nowPlayingEntries, error: _error),
            const RecentActivityCard(),
          ]),
        )
      ],
    );
  }
}
