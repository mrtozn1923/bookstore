import 'package:drift_flutter/drift_flutter.dart';
import 'package:flutter/foundation.dart';
import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app.dart';
import 'config/api_config.dart';
import 'config/dependencies.dart';
import 'data/repositories/auth_repository_api.dart';
import 'data/services/api_client.dart';
import 'data/services/app_database.dart';
import 'data/services/token_store_secure.dart';

Future<void> main() async {
  // Plugins (secure storage, preferences) need the binding before runApp
  WidgetsFlutterBinding.ensureInitialized();
  final api = ApiClient(
    baseUrl: Uri.parse(apiBaseUrl),
    tokens: SecureTokenStore(),
  );
  final auth = ApiAuthRepository(api, SharedPreferencesAsync());
  await auth.restore();
  // drift on the web needs sqlite3.wasm and a worker; this series targets
  // Android and iOS, so the web build keeps the catalog and cart in memory
  final db = kIsWeb ? null : AppDatabase(driftDatabase(name: 'bookstore'));
  runApp(
    MultiProvider(
      providers: apiProviders(api: api, auth: auth, db: db),
      child: const BookstoreApp(),
    ),
  );
}
