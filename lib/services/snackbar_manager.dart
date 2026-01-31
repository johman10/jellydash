import 'package:flutter/material.dart';

/// Centralized snackbar manager that ensures consistent behavior across the app.
/// Automatically dismisses any existing snackbar before showing a new one.
class SnackbarManager {
  SnackbarManager._();

  static final SnackbarManager _instance = SnackbarManager._();
  static SnackbarManager get instance => _instance;

  static const Duration defaultDuration = Duration(seconds: 2);

  Duration? _currentSnackbarDuration;

  /// Shows a snackbar with the given [message] in the provided [context].
  /// If a (persistent) snackbar is already visible, it will be dismissed first.
  /// [duration] specifies how long the snackbar should be visible, for persistent snackbar use Duration(days: 365).
  /// [action] can be provided to add an action button to the snackbar.
  void show(BuildContext context, String message,
      {Duration duration = defaultDuration, SnackBarAction? action}) {
    // Dismiss previous snackbar(s) if it's still visible
    dismiss(context);

    final screenWidth = MediaQuery.of(context).size.width;
    final snackbarWidth = screenWidth > 532 ? 500.0 : screenWidth - 32;

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
          content: Text(message),
          duration: duration,
          behavior: SnackBarBehavior.floating,
          width: snackbarWidth,
          action: action),
    );

    _currentSnackbarDuration = duration;
  }

  /// Dismisses the currently visible snackbar in the provided [context].
  /// Only dismisses persistent snackbars (Duration(days: 365)).
  void dismiss(BuildContext context) {
    if ((_currentSnackbarDuration?.inDays ?? 0) < 365) {
      return;
    }
    ScaffoldMessenger.of(context).clearSnackBars();
    _currentSnackbarDuration = null;
  }
}
