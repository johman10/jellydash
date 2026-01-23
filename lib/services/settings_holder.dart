import 'package:flutter/widgets.dart';

import 'app_settings_service.dart';

class SettingsHolder extends InheritedNotifier<ValueNotifier<AppSettings>> {
  const SettingsHolder({
    super.key,
    required ValueNotifier<AppSettings> super.notifier,
    required super.child,
  });

  static ValueNotifier<AppSettings>? maybeNotifierOf(BuildContext context) =>
      context.dependOnInheritedWidgetOfExactType<SettingsHolder>()?.notifier;

  static ValueNotifier<AppSettings> notifierOf(BuildContext context) =>
      maybeNotifierOf(context) ??
      (throw StateError('No SettingsHolder found in widget tree'));

  static AppSettings of(BuildContext context) => notifierOf(context).value;
}
