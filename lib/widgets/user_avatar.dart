import 'package:flutter/material.dart';
import 'package:jellydash/theme/jellydash_theme.dart';

class UserAvatar extends StatelessWidget {
  final String userName;
  final String? userImageUrl;

  const UserAvatar({
    super.key,
    required this.userName,
    this.userImageUrl,
  });

  Color _colorFromString(String input) {
    int hash = 0;
    for (int i = 0; i < input.length; i++) {
      hash = input.codeUnitAt(i) + ((hash << 5) - hash);
    }
    int r = (hash & 0xFF);
    int g = ((hash >> 8) & 0xFF);
    int b = ((hash >> 16) & 0xFF);
    r = 100 + (r % 156);
    g = 100 + (g % 156);
    b = 100 + (b % 156);
    return Color.fromARGB(255, r, g, b);
  }

  Widget get userImageFallback {
    return Text(
      userName.substring(0, 1).toUpperCase(),
      style: JellydashTextStyles.userAvatarFallback,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: 'User: $userName',
      child: Container(
        width: 50,
        height: 35,
        decoration: BoxDecoration(
          color: _colorFromString(userName),
          shape: BoxShape.circle,
        ),
        alignment: Alignment.center,
        child: userImageUrl != null && userImageUrl!.isNotEmpty
            ? Image.network(
                userImageUrl!,
                width: 25,
                height: 25,
                fit: BoxFit.cover,
                loadingBuilder: (context, child, loadingProgress) {
                  if (loadingProgress == null) return child;
                  return userImageFallback;
                },
                errorBuilder: (context, error, stackTrace) => userImageFallback,
              )
            : userImageFallback,
      ),
    );
  }
}
