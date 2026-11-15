import 'package:material_ui/material_ui.dart';
import 'package:provider/provider.dart';

import 'app.dart';
import 'config/api_config.dart';
import 'config/dependencies.dart';
import 'data/services/api_client.dart';

void main() {
  final api = ApiClient(baseUrl: Uri.parse(apiBaseUrl));
  runApp(
    MultiProvider(providers: apiProviders(api), child: const BookstoreApp()),
  );
}
