/// Address of Bookstore.Api, fixed at build time:
/// flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
const apiBaseUrl = String.fromEnvironment(
  'API_BASE_URL',
  defaultValue: 'http://127.0.0.1:5080',
);
