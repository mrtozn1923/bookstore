# Order totals per status; keeps working on Windows shells because nothing needs quoting
map({status, total: ([.items[] | .quantity * .unitPrice] | add)})
| group_by(.status)
| map({status: .[0].status, orders: length, total: (map(.total) | add)})
