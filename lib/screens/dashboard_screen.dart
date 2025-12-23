import 'package:flutter/material.dart';
import 'package:focus_detector/focus_detector.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';
import 'package:jellydash/types/session.dart';
import 'dart:async';
import '../services/app_settings_service.dart';
import '../services/jellyfin_api_service.dart';
import '../widgets/current_activities.dart';
import '../widgets/recent_activity_card.dart';

class DashboardScreen extends StatefulWidget {
  final JellyfinApiService? apiService;
  const DashboardScreen({super.key, this.apiService});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late JellyfinApiService _apiService;
  List<Session> _sessions = [];
  bool _initialLoading = true;
  int _pollingInterval = 10;
  Timer? _pollingTimer;

  Future<void> _startPolling() async {
     _pollingTimer?.cancel();

    final service = AppSettingsService();
    final baseUrl = await service.loadJellyfinBaseUrl();
    final apiKey = await service.loadJellyfinApiKey();
    final pollingInterval = await service.loadPollingInterval();

    setState(() {
      _pollingInterval = pollingInterval;
      _apiService = JellyfinApiService(baseUrl: baseUrl, apiKey: apiKey);
      _initialLoading = true;
    });

    _fetchSessions();
    _pollingTimer = Timer.periodic(Duration(seconds: _pollingInterval), (timer) {
      _fetchSessions();
    });
  }

  void _fetchSessions() {
    _apiService.fetchCurrentSessions().then((data) {
      setState(() {
        _sessions = data;
        _initialLoading = false;
      });
    });
  }

  void _stopPolling() {
    _pollingTimer?.cancel();
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      children: [
        FocusDetector(
          onFocusLost: _stopPolling,
          onFocusGained: _startPolling,
          child: SizedBox(
            width: double.infinity,
            child: Column(spacing: 16, children: [
              CurrentActivities(
                  isLoading: _initialLoading, sessions: _sessions),
              const RecentActivityCard(),
            ]),
          ),
        ),
      ],
    );
  }
}
