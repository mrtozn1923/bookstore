using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Web;

namespace Bookstore.BackOffice
{
    // GET /prices.ashx: the price list the store staff export every morning
    public sealed class PriceListHandler : IHttpHandler
    {
        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["BookstoreDb"].ConnectionString;
            StringBuilder csv = new StringBuilder("Isbn;Title;PriceTry\r\n");

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT Isbn, Title, PriceTry FROM dbo.Books ORDER BY Title", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        csv.Append(reader.GetString(0)).Append(';')
                           .Append(reader.GetString(1)).Append(';')
                           .Append(reader.GetDecimal(2).ToString(CultureInfo.InvariantCulture))
                           .Append("\r\n");
                    }
                }
            }

            context.Response.ContentType = "text/csv; charset=utf-8";
            context.Response.Write(csv.ToString());
        }
    }
}
