import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/services/api_exceptions.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'dart:async';
import '../services/api_service.dart';
import '../widgets/error_message.dart';
import '../widgets/now_playing.dart';
import '../widgets/recent_activities.dart';

class DashboardScreen extends StatefulWidget {
  final ApiService apiService;
  final bool usePluginApi;
  final int pollingInterval;

  const DashboardScreen({
    super.key,
    required this.pollingInterval,
    required this.usePluginApi,
    required this.apiService,
  });

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  List<PlaybackEntry> _nowPlayingEntries = [];
  List<PlaybackEntry> _historyEntries = [];
  bool _initialLoading = true;
  Exception? _initialError;
  Exception? _refreshError;
  bool _pollingPaused = false;
  bool _hasShownStaleToast = false;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _startPolling();
  }

  @override
  void didUpdateWidget(covariant DashboardScreen oldWidget) {
    super.didUpdateWidget(oldWidget);

    if (oldWidget.pollingInterval != widget.pollingInterval ||
        oldWidget.usePluginApi != widget.usePluginApi ||
        oldWidget.apiService != widget.apiService) {
      _startPolling();
    }
  }

  Future<void> _startPolling() async {
    _pollingTimer?.cancel();

    setState(() {
      _pollingPaused = false;
      _hasShownStaleToast = false;
      _initialError = null;
      _refreshError = null;
      // Only show a blocking loading state when we don't have any data yet.
      _initialLoading = _nowPlayingEntries.isEmpty;
    });

    _fetchNowPlaying();
    _pollingTimer =
        Timer.periodic(Duration(seconds: widget.pollingInterval), (timer) {
      _fetchNowPlaying();
    });
  }

  void _showSnackBar(String message) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }

  void _navigateToSettings() {
    GoRouter.of(context).push('/settings');
  }

  Future<void> _fetchNowPlaying() async {
    try {
      final data = await widget.apiService.fetchActivity(true, 20, null);
      if (!mounted) return;

      setState(() {
        _nowPlayingEntries =
            data.items.where((entry) => entry.isCompleted == false).toList();
        _historyEntries =
            data.items.where((entry) => entry.isCompleted == true).toList();
        _initialLoading = false;
        _initialError = null;
        _refreshError = null;
        _pollingPaused = false;
        _hasShownStaleToast = false;
      });

      ScaffoldMessenger.of(context).hideCurrentSnackBar();
    } catch (error) {
      final exception =
          error is Exception ? error : Exception(error.toString());
      final presentation =
          presentApiError(exception, usePluginApi: widget.usePluginApi);

      final hadData =
          _nowPlayingEntries.isNotEmpty || _historyEntries.isNotEmpty;
      final shouldStopPolling = presentation.isFatal;

      if (!mounted) return;
      setState(() {
        _initialLoading = false;
        if (hadData) {
          _refreshError = exception;
        } else {
          _initialError = exception;
        }

        if (shouldStopPolling) {
          _pollingPaused = true;
          _pollingTimer?.cancel();
        }
      });

      // When we have older data, keep it visible and show a clear message that
      // the refresh failed. Avoid spamming repeated snackbars.
      if (hadData && !_hasShownStaleToast) {
        if (shouldStopPolling) {
          _hasShownStaleToast = true;
          _showSnackBar(
            '${presentation.shortMessage}. Refresh paused — update Settings and retry.',
          );
          return;
        }

        _hasShownStaleToast = true;
        _showSnackBar(
          'Failed to refresh. Showing last known activity.',
        );
      }
    }
  }

  Future<void> _handleRetry() async {
    await _startPolling();
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

    final showFullError = !_initialLoading &&
        (_nowPlayingEntries.isEmpty || _historyEntries.isEmpty) &&
        _initialError != null;

    final showRefreshPausedError = _pollingPaused &&
        (_nowPlayingEntries.isNotEmpty || _historyEntries.isNotEmpty) &&
        _refreshError != null;

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
        if (showFullError)
          ErrorMessage(
            error: _initialError!,
            usePluginApi: widget.usePluginApi,
            onRetry: _pollingPaused ? _handleRetry : null,
            onGoToSettings: _pollingPaused ? _navigateToSettings : null,
          )
        else
          SizedBox(
            width: double.infinity,
            child: Column(
              spacing: 16,
              children: [
                if (showRefreshPausedError)
                  ErrorMessage(
                    error: _refreshError!,
                    usePluginApi: widget.usePluginApi,
                    onRetry: _handleRetry,
                    onGoToSettings: _navigateToSettings,
                  ),
                NowPlaying(
                  isLoading: _initialLoading,
                  nowPlayingEntries: _nowPlayingEntries,
                  // Do not replace historic data with an error on refresh
                  // failures. The dashboard will instead show a toast/snackbar.
                  error: _nowPlayingEntries.isEmpty ? _initialError : null,
                ),
                RecentActivities(
                  isLoading: _initialLoading,
                  historyEntries: _historyEntries,
                  // Do not replace historic data with an error on refresh
                  // failures. The dashboard will instead show a toast/snackbar.
                  error: _historyEntries.isEmpty ? _initialError : null,
                ),
              ],
            ),
          ),
      ],
    );
  }
}
