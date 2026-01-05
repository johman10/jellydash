import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:go_router/go_router.dart';
import 'package:jellydash/scaffolds/app_scaffold.dart';

typedef RefreshCallback = Future<void> Function();

GoRouter _testRouter(Widget child) {
  return GoRouter(
    routes: [
      GoRoute(
        path: '/',
        builder: (context, state) => child,
      ),
    ],
    initialLocation: '/',
  );
}

void main() {
  testWidgets('renders title and children', (tester) async {
    final router = _testRouter(AppScaffold(
      title: 'Test Title',
      children: [const Text('Child Widget')],
    ));
    await tester.pumpWidget(MaterialApp.router(
      routerDelegate: router.routerDelegate,
      routeInformationParser: router.routeInformationParser,
      routeInformationProvider: router.routeInformationProvider,
    ));
    expect(find.text('Test Title'), findsOneWidget);
    expect(find.text('Child Widget'), findsOneWidget);
  });

  testWidgets('renders actions', (tester) async {
    final router = _testRouter(AppScaffold(
      title: 'Test',
      actions: [IconButton(icon: const Icon(Icons.add), onPressed: () {})],
      children: [const SizedBox()],
    ));
    await tester.pumpWidget(MaterialApp.router(
      routerDelegate: router.routerDelegate,
      routeInformationParser: router.routeInformationParser,
      routeInformationProvider: router.routeInformationProvider,
    ));
    expect(find.byIcon(Icons.add), findsOneWidget);
  });

  testWidgets('calls onRefresh when pulled', (tester) async {
    bool refreshed = false;
    Future<void> onRefresh() async {
      refreshed = true;
    }
    final router = _testRouter(AppScaffold(
      title: 'Test',
      onRefresh: onRefresh,
      children: [
        Container(
          height: 10,
          color: Colors.red,
        ),
      ],
    ));
    await tester.pumpWidget(MaterialApp.router(
      routerDelegate: router.routerDelegate,
      routeInformationParser: router.routeInformationParser,
      routeInformationProvider: router.routeInformationProvider,
    ));
    // Use fling to trigger RefreshIndicator
    await tester.fling(find.byType(Container), const Offset(0, 300), 1000);
    await tester.pumpAndSettle();
    expect(refreshed, isTrue);
  });
}
