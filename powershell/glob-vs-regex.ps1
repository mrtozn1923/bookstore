$files = 'OrderService.cs', 'OrderService.cs.bak', 'Orders.csv', 'app.log'

# -like takes a wildcard pattern, -match a .NET regex
$files | Where-Object { $_ -like '*.cs' }
'---'
$files | Where-Object { $_ -match '.cs' }
'---'
$files | Where-Object { $_ -match '\.cs$' }
'---'
# a glob given to -match is an invalid regex
try { 'OrderService.cs' -match '*.cs' } catch { $_.Exception.Message }
