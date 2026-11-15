import 'package:material_ui/material_ui.dart';

import 'ui/core/themes/theme.dart';
import 'ui/home/widgets/home_screen.dart';

class BookstoreApp extends StatelessWidget {
  const BookstoreApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Bookstore',
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.system,
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      supportedLocales: const [Locale('en'), Locale('tr')],
      home: const HomeScreen(),
    );
  }
}
