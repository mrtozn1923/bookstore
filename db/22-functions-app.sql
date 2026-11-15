/* Post 22 - SECURITY DEFINER, as the app role: psql -U bookstore_app -d bookstore -f /lab/22-functions-app.sql */
\pset footer off
UPDATE stock_items SET quantity_on_hand = 500 WHERE book_id = 1;
SELECT adjust_stock(1, 5, 'warehouse count 2026-10-21');
SELECT adjust_stock(1, -2, '');
SELECT book_id, delta, reason, adjusted_by FROM stock_adjustments;
SELECT current_user, session_user;
