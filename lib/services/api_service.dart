import 'dart:async';

import 'package:jellydash/types/activity_response.dart';

abstract class ApiService {
  Future<ActivityResponse> fetchActivity(
      bool includeActive, int? limit, String? cursor);
}
