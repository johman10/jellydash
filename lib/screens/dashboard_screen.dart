import 'dart:developer';

import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/services/exceptions.dart';
import 'package:jellydash/services/snackbar_manager.dart';
import 'package:jellydash/types/playback_entry.dart';
import 'package:jellydash/widgets/dashboard_section.dart';
import 'dart:async';
import '../services/api_service.dart';

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
  CustomException? _exception;
  int _errorCount = 0;

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
      setState(() {
        _exception = null;
      });
      _startPolling();
    }
  }

  Future<void> _startPolling() async {
    _pollingTimer?.cancel();

    _fetchActivity();
    _pollingTimer =
        Timer.periodic(Duration(seconds: widget.pollingInterval), (timer) {
      _fetchActivity();
    });
  }

  Future<void> _fetchActivity() async {
    try {
      if (_exception != null && _exception!.fatal == true) {
        _pollingTimer?.cancel();
        log("Fatal exception encountered, interrupt polling");
        return;
      }

      final data = await widget.apiService.fetchActivity(true, 20, null);

      if (mounted) {
        SnackbarManager.instance.dismiss(context);
      }

      setState(() {
        _nowPlayingEntries =
            data.items.where((entry) => entry.isCompleted == false).toList();
        _historyEntries =
            data.items.where((entry) => entry.isCompleted == true).toList();
        _initialLoading = false;
        _exception = null;
        _errorCount = 0;
      });
    } catch (error) {
      // If there was an exception before, show a snackbar, but don't show a second one
      if (_errorCount == 1) {
        _showErrorSnackBar(error as CustomException);
      }

      setState(() {
        _initialLoading = false;
        _exception = error as CustomException;
        _errorCount += 1;
      });
    }
  }

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  String get historyEmptyMessage {
    if (widget.usePluginApi) {
      return "History doesn't write itself! Go watch!";
    } else {
      return "No recent activities. Enable the Jellydash plugin API for more detailed history.";
    }
  }

  void _showErrorSnackBar(CustomException error) {
    var message = "An unknown error occurred: ${error.toString()}";
    if (error is NetworkException) {
      if (error.type == NetworkExceptionType.timeout) {
        message = "Connection timed out. Please check your server status.";
      } else if (error.type == NetworkExceptionType.connection) {
        message = "Could not connect to the server. Check your network.";
      } else if (error.type == NetworkExceptionType.unknown) {
        message = "An unknown network error occurred.";
      }
    } else if (error is NotFoundException) {
      if (widget.usePluginApi) {
        message =
            "Endpoint not found, check your base URL and whether the plugin API is installed.";
      } else {
        message = "Endpoint not found, check your base URL.";
      }
    } else if (error is UnauthorizedException) {
      message = "Your API key seems wrong.";
    }
    SnackbarManager.instance.show(context, message,
        duration: Duration(days: 365),
        action: SnackBarAction(
          label: 'Dismiss',
          onPressed: () {
            SnackbarManager.instance.dismiss(context);
          },
        ));
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
      onRefresh: isTouchDevice ? _fetchActivity : null,
      children: [
        SizedBox(
          width: double.infinity,
          child: Column(
            spacing: 16,
            children: [
              DashboardSection(
                isLoading: _initialLoading,
                entries: _nowPlayingEntries,
                title: "Now Playing",
                emptyMessage: 'It\'s quiet... too quiet.',
              ),
              DashboardSection(
                isLoading: _initialLoading,
                entries: _historyEntries,
                title: "Recent Activities",
                emptyMessage: historyEmptyMessage,
              )
            ],
          ),
        ),
      ],
    );
  }
}
