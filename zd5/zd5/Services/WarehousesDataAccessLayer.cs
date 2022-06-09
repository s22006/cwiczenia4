using System.Data;
using System.Data.SqlClient;
using zd5.Dtos;

namespace zd5.Services
{
    public interface IWarehousesDataAccessLayer
    {
        Task UpdateFulfilledAtDateAsync(int productId, int amount);

        Task<int> CreateOrderAsync(WarehousesDto dto);

        Task<decimal> AddWarhouseDataAsync(WarehousesDto dto);
    }

    public class WarehousesDataAccessLayer : IWarehousesDataAccessLayer
    {
        private readonly IConfiguration _configuration;
        private string CONNECTION_STRING = string.Empty;

        public WarehousesDataAccessLayer(IConfiguration configuration)
        {
            _configuration = configuration;
            CONNECTION_STRING = _configuration["ConnectionStrings:Default"];
        }
 
        public async Task<int> CreateOrderAsync(WarehousesDto request)
        {
            using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
            {
                string queryStringOrder = $"SELECT * FROM [zd5].[dbo].[Order] WHERE IdProduct = {request.IdProduct} AND Amount = {request.Amount};";

                SqlCommand command = new SqlCommand(queryStringOrder, connection);

                connection.Open();

                int idOrder = 0;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    int.TryParse(reader["IdOrder"].ToString(), out idOrder);
                    reader.Close();
                }

                string queryStringProduct = $"SELECT * FROM [zd5].[dbo].[Product] WHERE IdProduct = {request.IdProduct};";
                command = new SqlCommand(queryStringProduct, connection);
                command.ExecuteNonQuery();
                decimal productPrice = 0;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    decimal.TryParse(reader["Price"].ToString(), out productPrice);
                    reader.Close();
                }
                 

                ProductWarehouseDto pw = new ProductWarehouseDto()
                {
                    IdWarehouse = request.IdWarehouse,
                    IdProduct = request.IdProduct,
                    IdOrder = idOrder,
                    Amount = request.Amount,
                    CreatedAt = DateTime.Now,
                    Price = request.Amount * productPrice,
                };

                string queryStringInsert = @"INSERT INTO [zd5].[dbo].[Product_Warehouse] (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)" +
                    $"VALUES (@IdWarehouse,@IdProduct,@IdOrder,@Amount,@Price,'{pw.CreatedAt}');";

                int insertedRowId = 0;

                using (SqlCommand cmd = new SqlCommand(queryStringInsert, connection))
                {
 
                    cmd.Parameters.AddWithValue("@IdWarehouse", pw.IdWarehouse);
                    cmd.Parameters.AddWithValue("@IdProduct", pw.IdProduct);
                    cmd.Parameters.AddWithValue("@IdOrder", pw.IdOrder);
                    cmd.Parameters.AddWithValue("@Amount", pw.Amount);
                    cmd.Parameters.AddWithValue("@Price", pw.Price.ToString().Replace(',', '.'));

                    insertedRowId = Convert.ToInt32(cmd.ExecuteScalar());
                    connection.Close();
                    return insertedRowId;
                }
            }
        }

        public async Task UpdateFulfilledAtDateAsync(int productId, int amount)
        {
            using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand command;

                string? IdOrder = GetOrderIDAsync(productId, amount, connection, out command, out IdOrder);

                string queryStringUpdate = $"UPDATE [zd5].[dbo].[Order] SET " +
                    $"FulfilledAt = '{DateTime.Now}' WHERE IdOrder = {IdOrder};";

                command = new SqlCommand(queryStringUpdate, connection);
                command.ExecuteNonQuery();

                connection.Close();
            }
        }

        public Task<decimal> AddWarhouseDataAsync(WarehousesDto dto)
        {
            using (SqlConnection con = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand cmd = new SqlCommand("dbo.AddProductToWarehouse", con)
                {
                    CommandType = CommandType.StoredProcedure
                };
 
                cmd.Parameters.AddWithValue("@IdProduct", dto.IdProduct);
                cmd.Parameters.AddWithValue("@IdWarehouse", dto.IdWarehouse);
                cmd.Parameters.AddWithValue("@Amount", dto.Amount);
                cmd.Parameters.AddWithValue("@CreatedAt", dto.CreatedAt);

                con.Open();

                decimal id = (decimal)cmd.ExecuteScalar();

                con.Close();

                return Task.FromResult(id);
            }
        }

        private static string GetOrderIDAsync(int productId, int amount, SqlConnection connection, out SqlCommand command, out string? IdOrder)
        {
            string queryStringOrder = $"SELECT * FROM [zd5].[dbo].[Order] WHERE IdProduct = {productId} AND Amount = {amount};";


            command = new SqlCommand(queryStringOrder, connection);
            connection.Open();

            IdOrder = string.Empty;
            using (SqlDataReader reader = command.ExecuteReader())
            {
                reader.Read();
                IdOrder = reader["IdOrder"].ToString();
                reader.Close();
            }

            return IdOrder;
        }
    }
}
