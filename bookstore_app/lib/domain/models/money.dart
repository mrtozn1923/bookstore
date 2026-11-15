/// Amount in kurus (1 TRY = 100 kurus). At run time this is a plain int.
extension type const Money(int kurus) {
  static const zero = Money(0);

  Money operator +(Money other) => Money(kurus + other.kurus);
  Money operator *(int quantity) => Money(kurus * quantity);

  String format() =>
      '${kurus ~/ 100}.${(kurus % 100).toString().padLeft(2, '0')} TRY';
}
