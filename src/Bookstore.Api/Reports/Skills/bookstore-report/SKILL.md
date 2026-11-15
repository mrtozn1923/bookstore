---
name: bookstore-report
description: Bookstore's conventions for management reports built from sales data. Use when creating an Excel or PDF report about Bookstore sales, revenue or stock.
---

# Bookstore report conventions

## Workbook layout

- First sheet: `Summary` with the month, total revenue, number of orders and number of books sold.
- Second sheet: `Daily revenue` with one row per day and a line chart of revenue.
- Third sheet: `Top titles` with the ten best-selling titles, sorted by revenue.
- If stock data is available, a fourth sheet: `Stock` with the current stock of the top titles.

## Formatting

- Amounts are in Turkish lira; use the number format `#,##0.00 "TRY"`.
- Dates use `yyyy-mm-dd`.
- Freeze the header row on every sheet.

## Rules

- Use only the data in the uploaded files and the tool results. Never invent numbers.
- Do not add customer names, e-mail addresses or other personal data to any sheet.
- Name the file `sales-report-YYYY-MM.xlsx` and save it to `$OUTPUT_DIR`.
