curl -X POST \
  http://localhost:80/api/system/simulation \
  -H 'Cache-Control: no-cache' \
  -H 'Content-Type: application/json' \
  -H 'Postman-Token: a4c68858-4995-f72e-6e7e-e726aecb42f2' \
  -d '{
	"ClientName": "Raintree",
	"ClientAccountsFilePath": "/demo/Raintree.txt",
	"ObligationsFilePath":"/demo/Obligations/Raintree.txt"
}'
