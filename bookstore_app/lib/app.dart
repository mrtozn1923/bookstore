import 'package:go_router/go_router.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import 'routing/router.dart';
import 'ui/core/themes/theme.dart';

class BookstoreApp extends StatefulWidget {
  const BookstoreApp({super.key, this.initialLocation});

  final String? initialLocation;

  @override
  State<BookstoreApp> createState() => _BookstoreAppState();
}

class _BookstoreAppState extends State<BookstoreApp> {
  // Created once; a new GoRouter on every build would reset navigation
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    _router = createRouter(
      auth: context.read(),
      initialLocation: widget.initialLocation,
    );
  }

  @override
  void dispose() {
    _router.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'Bookstore',
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.system,
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      supportedLocales: const [Locale('en'), Locale('tr')],
      routerConfig: _router,
    );
  }
}
