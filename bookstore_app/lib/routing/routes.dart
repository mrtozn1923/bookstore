abstract final class Routes {
  static const catalog = '/catalog';
  static String book(String isbn) => '/catalog/books/$isbn';
  static const cart = '/cart';
  static const address = '/cart/address';
  static const orders = '/orders';
  static const signIn = '/sign-in';
}
