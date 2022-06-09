using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using zd5.Dtos;
using zd5.Services;
using zd5.Validators;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace zd5.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Warehouses2Controller : ControllerBase
    {
        private readonly IWarehousesDataAccessLayer _dal;

        private readonly IConfiguration _configuration;

        private string CONNECTION_STRING = String.Empty;

        public Warehouses2Controller(IWarehousesDataAccessLayer dal, IConfiguration configuration)
        {
            _dal = dal;

            _configuration = configuration;
            CONNECTION_STRING = _configuration["ConnectionStrings:Default"];
        }

        // POST api/<Warehouses2Controller>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] WarehousesDto request)
        {
            WarehousesDtoValidator validator = new WarehousesDtoValidator(CONNECTION_STRING);
            ValidationResult result = validator.Validate(request);

            if (!result.IsValid)
            {
                string allErrorMessages = JsonConvert.SerializeObject(result.Errors.Select(x =>
                new CustomErrorDto
                {
                    PropertyName = String.IsNullOrEmpty(x.PropertyName) ? "Logic error" : x.PropertyName,
                    ErrorMessage = x.ErrorMessage
                }), Formatting.Indented);

                return BadRequest(allErrorMessages);
            }

            decimal id = await _dal.AddWarhouseDataAsync(request);

            return Ok($"Id = {id}");
        }
    }
}
