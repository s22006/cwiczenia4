using System.ComponentModel.DataAnnotations;

namespace zd5.Dtos
{
    // Use FluentValidation for validation requirements
    public class WarehousesDto
    {
        public int IdProduct { get; set; }

        public int IdWarehouse { get; set; }

        public int Amount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
