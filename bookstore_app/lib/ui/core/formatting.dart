import 'package:intl/intl.dart';
import 'package:material_ui/material_ui.dart';

import '../../domain/models/money.dart';

/// Formats money and dates for the locale the app is shown in.
extension LocaleFormatting on BuildContext {
  String _locale() => Localizations.localeOf(this).toLanguageTag();

  String formatPrice(Money price) => NumberFormat.currency(
    locale: _locale(),
    name: 'TRY',
    symbol: '₺',
  ).format(price.kurus / 100);

  String formatDate(DateTime date) => DateFormat.yMMMMd(_locale()).format(date);
}
