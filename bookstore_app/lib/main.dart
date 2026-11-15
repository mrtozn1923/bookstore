import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'config/dependencies.dart';

void main() {
  runApp(MultiProvider(providers: appProviders(), child: const BookstoreApp()));
}
