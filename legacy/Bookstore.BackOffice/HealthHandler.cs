using System.Configuration;
using System.Data.SqlClient;
using System.Web;

namespace Bookstore.BackOffice
{
    // GET /health.ashx: answers only when the database is reachable
    public sealed class HealthHandler : IHttpHandler
    {
        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            string connectionString = ConfigurationManager.ConnectionStrings["BookstoreDb"].ConnectionString;
            context.Response.ContentType = "text/plain";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("SELECT 1", connection))
                {
                    connection.Open();
                    command.ExecuteScalar();
                }
                context.Response.Write("Healthy");
            }
            catch (SqlException)
            {
                context.Response.StatusCode = 503;
                context.Response.Write("Unhealthy");
            }
        }
    }
}
