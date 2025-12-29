import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:shared_preferences/shared_preferences.dart';

class AppSettings {
  final String jellyfinBaseUrl;
  final String jellyfinApiKey;
  final int pollingInterval;

  AppSettings({
    required this.jellyfinBaseUrl,
    required this.jellyfinApiKey,
    required this.pollingInterval,
  });
}

class AppSettingsService {
  static final AppSettingsService _instance = AppSettingsService._internal();
  factory AppSettingsService() => _instance;
  AppSettingsService._internal();

  static const String themeModeKey = 'themeMode';
  static const String jellyfinBaseUrlKey = 'jellyfin_baseUrl';
  static const String jellyfinApiKeyKey = 'jellyfin_apiKey';
  static const String pollingIntervalKey = 'pollingInterval';

  SharedPreferences? _prefs;

  /// Call this once at app startup before using other methods.
  Future<void> init() async {
    _prefs = await SharedPreferences.getInstance();
  }

  /// Loads the Jellyfin base URL.
  Future<String> loadJellyfinBaseUrl() async {
    return _prefs?.getString(jellyfinBaseUrlKey) ??
        dotenv.get('DEFAULT_JELLYFIN_HOST', fallback: 'http://localhost:8096');
  }

  /// Saves the Jellyfin base URL.
  Future<void> saveJellyfinBaseUrl(String url) async {
    await _prefs?.setString(jellyfinBaseUrlKey, url);
  }

  /// Loads the Jellyfin API key.
  Future<String> loadJellyfinApiKey() async {
    return _prefs?.getString(jellyfinApiKeyKey) ??
        dotenv.get('DEFAULT_JELLYFIN_API_KEY', fallback: '');
  }

  /// Saves the Jellyfin API key.
  Future<void> saveJellyfinApiKey(String apiKey) async {
    await _prefs?.setString(jellyfinApiKeyKey, apiKey);
  }

  /// Loads the polling interval (seconds).
  Future<int> loadPollingInterval() async {
    return _prefs?.getInt(pollingIntervalKey) ??
        int.parse(dotenv.get('DEFAULT_POLLING_INTERVAL', fallback: '10'));
  }

  /// Saves the polling interval (seconds).
  Future<void> savePollingInterval(int interval) async {
    await _prefs?.setInt(pollingIntervalKey, interval);
  }

  Future<AppSettings> loadSettings() async {
    final baseUrl = await loadJellyfinBaseUrl();
    final apiKey = await loadJellyfinApiKey();
    final pollingInterval = await loadPollingInterval();

    return AppSettings(
      jellyfinBaseUrl: baseUrl,
      jellyfinApiKey: apiKey,
      pollingInterval: pollingInterval,
    );
  }
}
