import 'package:shared_preferences/shared_preferences.dart';

/// Service for loading and saving application settings.
/// Uses shared_preferences for persistent storage.

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

  /// Loads the theme mode setting ("light", "dark", or "system").
  Future<String> loadThemeMode() async {
    return _prefs?.getString(themeModeKey) ?? 'system';
  }

  /// Saves the theme mode setting.
  Future<void> saveThemeMode(String mode) async {
    await _prefs?.setString(themeModeKey, mode);
  }

  /// Loads the Jellyfin base URL.
  Future<String> loadJellyfinBaseUrl() async {
    return _prefs?.getString(jellyfinBaseUrlKey) ?? 'http://localhost:8096';
  }

  /// Saves the Jellyfin base URL.
  Future<void> saveJellyfinBaseUrl(String url) async {
    await _prefs?.setString(jellyfinBaseUrlKey, url);
  }

  /// Loads the Jellyfin API key.
  Future<String> loadJellyfinApiKey() async {
    return _prefs?.getString(jellyfinApiKeyKey) ?? '';
  }

  /// Saves the Jellyfin API key.
  Future<void> saveJellyfinApiKey(String apiKey) async {
    await _prefs?.setString(jellyfinApiKeyKey, apiKey);
  }

  /// Loads the polling interval (seconds).
  Future<int> loadPollingInterval() async {
    return _prefs?.getInt(pollingIntervalKey) ?? 10;
  }

  /// Saves the polling interval (seconds).
  Future<void> savePollingInterval(int interval) async {
    await _prefs?.setInt(pollingIntervalKey, interval);
  }
}
