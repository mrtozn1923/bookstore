// Bodies copied from real Bookstore.Api responses (SQLite, Development).

const booksPage1 =
    '{"items":[{"isbn":"9780451524935","title":"1984","author":"George Orwell",'
    '"category":"Dystopian","price":279.0,"publishedYear":1949},'
    '{"isbn":"9780143035008","title":"Anna Karenina","author":"Leo Tolstoy",'
    '"category":"Classics","price":429.0,"publishedYear":1878}],'
    '"total":20,"page":1,"pageSize":2}';

const dune =
    '{"isbn":"9780441172719","title":"Dune","author":"Frank Herbert",'
    '"category":"Science Fiction","price":489.0,"publishedYear":1965,'
    '"description":"A desert planet, a noble family and the most valuable '
    'substance in the universe.","available":25}';

const notFound =
    '{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.5",'
    '"title":"Not Found","status":404}';

const customerNameTooLong =
    '{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",'
    '"title":"One or more validation errors occurred.","status":400,'
    '"errors":{"CustomerName":["The field CustomerName must be a string with '
    'a maximum length of 200."]}}';

const outOfStock =
    '{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.10",'
    '"title":"Conflict","status":409,'
    '"detail":"Not enough stock for: 9780060934347."}';

const unauthorized =
    '{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.2",'
    '"title":"Unauthorized","status":401}';

const newCart =
    '{"id":"01a1dde5-644f-7a17-8756-48f00ede6927","items":[],"total":0,'
    '"currency":"TRY"}';

const placedOrder =
    '{"id":1,"status":"Placed","placedAtUtc":"2026-11-15T06:24:51.201729Z",'
    '"items":[{"title":"Dune","unitPrice":489.0,"quantity":2,'
    '"lineTotal":978.0}],"total":978.0,"currency":"TRY"}';
