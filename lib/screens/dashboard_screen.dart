import 'package:flutter/material.dart';
import 'dart:async';
import '../services/app_settings_service.dart';
import '../services/jellyfin_api_service.dart';
import '../widgets/current_activity_card.dart';
import '../widgets/recent_activity_card.dart';
import '../widgets/app_drawer.dart';

class DashboardScreen extends StatefulWidget {
  final JellyfinApiService? apiService;
  const DashboardScreen({super.key, this.apiService});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late JellyfinApiService _apiService;
  List<dynamic>? _sessions;
  bool _initialLoading = true;
  int _pollingInterval = 10;
  Timer? _pollingTimer;

  @override
  void initState() {
    super.initState();
    _loadSettings();
    _startPolling();
  }

  Future<void> _loadSettings() async {
    final service = AppSettingsService();
    final baseUrl = await service.loadJellyfinBaseUrl();
    final apiKey = await service.loadJellyfinApiKey();
    final pollingInterval = await service.loadPollingInterval();
    setState(() {
      _pollingInterval = pollingInterval;
      _apiService = JellyfinApiService(baseUrl: baseUrl, apiKey: apiKey);
      _initialLoading = true;
    });
  }

  void _startPolling() {
    _pollingTimer?.cancel();
    _pollingTimer =
        Timer.periodic(Duration(seconds: _pollingInterval), (timer) {
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

  @override
  void dispose() {
    _pollingTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Jellydash'),
      ),
      drawer: const AppDrawer(),
      body: LayoutBuilder(
        builder: (context, constraints) {
          return Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 1200),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const SizedBox(height: 24),
                  // Current Activity Widget
                  CurrentActivityCard(
                    child: _initialLoading
                        ? const CircularProgressIndicator()
                        : (_sessions != null && _sessions!.isNotEmpty
                            ? Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: _sessions!.map((session) {
                                  final user = session['UserName'] ?? 'Unknown';
                                  final nowPlaying = session['NowPlayingItem']?['Name'] ?? 'Idle';
                                  return Text('$user: $nowPlaying');
                                }).toList(),
                              )
                            : const Text('No active sessions.')
                          ),
                  ),
                  const SizedBox(height: 24),
                  // Recent Activity Widget (placeholder)
                  const RecentActivityCard(),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
