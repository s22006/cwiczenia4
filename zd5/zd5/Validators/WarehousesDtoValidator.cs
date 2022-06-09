using System.Data.SqlClient;
using FluentValidation;
using zd5.Dtos;

namespace zd5.Validators
{
    public class WarehousesDtoValidator : AbstractValidator<WarehousesDto>
    {
        private static string CONNECTION_STRING = String.Empty;

        public WarehousesDtoValidator(string connectionString)
        {
            CONNECTION_STRING = connectionString;

            RuleFor(x => x.IdWarehouse)
                .Must((obj, request) => { return CheckDoesEntityExcist(obj.IdWarehouse, "dbo.Warehouse", "IdWarehouse"); })
                    .WithMessage($"Warehouse does not exist in database.")
                .NotNull();

            RuleFor(x => x.CreatedAt)
                .Must((obj, request) => { return CheckCreatedAt(obj.CreatedAt, obj.IdProduct, obj.Amount); })
                    .WithMessage($"CreatedAt should be less than OrderDate.")
                .NotNull();

            RuleFor(x => x.Amount)
                .Must(GrateThanZero)
                    .WithMessage($"Amount value should be between 1-2147483647.")
                .NotNull();

            RuleFor(x => x.IdProduct)
                .Must((obj, request) => { return CheckDoesEntityExcist(obj.IdProduct, "dbo.Product", "IdProduct"); })
                    .WithMessage($"Product does not exist in database.")
                .NotNull();

            RuleFor(x => new { x.IdProduct, x.Amount })
                .Must(x => CheckDoesOrderExcist(x.IdProduct, x.Amount))
                     .WithMessage("Order for this product and amount does not exist in database.");

            RuleFor(x => new { x.CreatedAt })
                .Must((obj, request) => { return CheckOrderRealized(obj.IdProduct, obj.Amount); })
                    .WithMessage($"The order has already been completed.");
        }

        //// Produkt możemy dodać do hurtowni tylko jeśli w tabeli Order istnieje zlecenie
        //// zakupu produktu.Sprawdzamy zatem czy w tabeli Order istnieje rekord z:
        //// IdProduct i Amount zgodnym z naszym żądaniem.
        private static bool CheckDoesOrderExcist(int productId, int amount)
        {
            string queryString = $"SELECT * FROM [zd5].[dbo].[Order] WHERE IdProduct = {productId} AND Amount = {amount};";

            using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand command = new SqlCommand(queryString, connection);

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                        return true;
                }
            }

            return false;
        }

        //// Sprawdzamy czy przypadkiem to zlecenie nie zostało już zrealizowane.
        //// Sprawdzamy czy w tabeli Product_Warehouse nie ma już wiersza z danym IdOrder
        private static bool CheckOrderRealized(int productId, int amount)
        {
            string queryString = $"SELECT * FROM [zd5].[dbo].[Order] WHERE IdProduct = {productId} AND Amount = {amount};";

            using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand command = new SqlCommand(queryString, connection);

                connection.Open();

                string? IdOrder = string.Empty;
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();
                    IdOrder = reader["IdOrder"].ToString();
                    reader.Close();
                }

                queryString = $"SELECT * FROM [zd5].[dbo].[Product_Warehouse] WHERE IdOrder = {IdOrder};";
                command = new SqlCommand(queryString, connection);
                bool realized = false;

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    realized = reader.HasRows ? false : true;
                }

                if (realized)
                    return true;
            }

            return false;
        }

        //// CreatedAt zamówienia powinno być mniejsze niż CreatedAt pochodzące z naszego żądania 
        //// (zamówienie/order powinno pojawić się w bazie danych wcześniej niż nasze żądanie)
        private static bool CheckCreatedAt(DateTime createdAt, int productId, int amount)
        {
            string queryString = $"SELECT * FROM [zd5].[dbo].[Order] WHERE IdProduct = {productId} AND Amount = {amount};";

            using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand command = new SqlCommand(queryString, connection);

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    reader.Read();

                    DateTime orderDate;
                    DateTime.TryParse(reader[nameof(WarehousesDto.CreatedAt)].ToString(), out orderDate);

                    if (createdAt < orderDate)
                        return true;
                }
            }

            return false;
        }

        //// Sprawdzamy czy produkt o podanym id istnieje. Następnie sprawdzamy czy hurtownia o podanym id istnieje.
        private static bool CheckDoesEntityExcist(int id, string tableName, string columnName)
        {
            string queryString = $"SELECT * FROM {tableName} WHERE {columnName} = {id};";

            using (SqlConnection connection = new SqlConnection(CONNECTION_STRING))
            {
                SqlCommand command = new SqlCommand(queryString, connection);

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                        return true;
                }
            }

            return false;
        }

        //// Ponadto upewniamy się, że wartość Amount jest większa od 0.
        private static bool GrateThanZero(int amount)
        {
            return amount > 0 ? true : false;
        }
    }
}
