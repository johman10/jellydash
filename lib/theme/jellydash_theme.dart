import 'package:flutter/material.dart';

class JellydashColors {
  JellydashColors._();

  static const Color primary = Colors.deepPurple;
  static const Color drawerHeaderBackground = primary;
  static const Color drawerHeaderText = Colors.white;

  static final Color posterFallbackBackground = Colors.grey.shade800;
  static const Color posterFallbackIcon = Colors.white54;

  static const Color pausedOverlayBackground = Colors.black45;
  static const Color pausedOverlayIcon = Colors.white;

  static const Color sliderActive = Colors.purpleAccent;
  static final Color sliderSecondaryActive = Colors.grey.shade700;

  static final Color transcodingBadgeBackground = Colors.grey.shade700;
}

class JellydashTextStyles {
  JellydashTextStyles._();

  static const TextStyle sectionTitle =
      TextStyle(fontSize: 20, fontWeight: FontWeight.bold);

  static const TextStyle appDrawerHeader = TextStyle(
    color: JellydashColors.drawerHeaderText,
    fontSize: 24,
  );

  static const TextStyle userAvatarFallback = TextStyle(
    fontSize: 16,
    color: JellydashColors.posterFallbackIcon,
  );
}
