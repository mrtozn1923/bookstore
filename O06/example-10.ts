type Book = {
  title: string;
  subtitle?: string;
};

type Subtitle = Book["subtitle"]; // string | undefined

const formats = ["paperback", "ebook"];
type Format = (typeof formats)[number]; // string

type StockByIsbn = Record<string, number>;
type Stock = StockByIsbn["anyTypo"]; // number, no complaint
