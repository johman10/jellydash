import 'package:flutter/material.dart';

/// Centralized snackbar manager that ensures consistent behavior across the app.
/// Automatically dismisses any existing snackbar before showing a new one.
class SnackbarManager {
  SnackbarManager._();

  static final SnackbarManager _instance = SnackbarManager._();
  static SnackbarManager get instance => _instance;

  static const Duration defaultDuration = Duration(seconds: 2);

  Duration? _currentSnackbarDuration;

  /// Shows a snackbar with the given message.
  /// Automatically closes the currently displayed snackbar only if it has a
  /// long duration (>= 5 seconds). Short-duration snackbars are allowed to
  /// finish naturally, queuing the new message.
  void show(BuildContext context, String message, {Duration duration = defaultDuration, SnackBarAction? action}) {
    // Only clear if the currently showing snackbar has a long duration
    if ((_currentSnackbarDuration?.inSeconds ?? 0) >= 5) {
      ScaffoldMessenger.of(context).clearSnackBars();
    }

    final screenWidth = MediaQuery.of(context).size.width;
    final snackbarWidth = screenWidth > 532 ? 500.0 : screenWidth - 32;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(message),
        duration: duration,
        behavior: SnackBarBehavior.floating,
        width: snackbarWidth,
        action: action
      ),
    );

    _currentSnackbarDuration = duration;
  }

  void dismiss(BuildContext context) {
    if ((_currentSnackbarDuration?.inSeconds ?? 0) <= 5) {
      return;
    }
    ScaffoldMessenger.of(context).clearSnackBars();
    _currentSnackbarDuration = null;
  }
}
