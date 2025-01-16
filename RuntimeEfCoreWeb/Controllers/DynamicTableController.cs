using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RuntimeEfCoreWeb.Models;

namespace RuntimeEfCoreWeb.Controllers
{
    public  class DynamicTableController : Controller
    {
        private readonly DynamicTableService _dynamicTableService;
       
        public DynamicTableController(IConfiguration configuration, DynamicTableService dynamicTableService)
        {
            DynamicContextExtensions.DynamicContext(configuration.GetConnectionString("DefaultConnection")!);
            _dynamicTableService = dynamicTableService;
        }

        // Tablo listesi
        public async Task<IActionResult> Index()
        {
            var tables = await _dynamicTableService.GetAllTables();
            return View(tables);
        }

        // Tablo detayı ve sütunları
        public async Task<IActionResult> Details(string tableName)
        {
            var columns = await _dynamicTableService.GetTableColumns(tableName);
            ViewBag.TableName = tableName;
            return View(columns);
        }

        // Sütun güncelleme
        [HttpPut]
        public async Task<IActionResult> UpdateColumn([FromBody] UpdateColumnRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TableName))
                    return BadRequest("Table name is required");

                if (string.IsNullOrWhiteSpace(request.OldColumnName))
                    return BadRequest("Old column name is required");

                if (string.IsNullOrWhiteSpace(request.NewColumnName))
                    return BadRequest("New column name is required");

                if (string.IsNullOrWhiteSpace(request.NewDataType))
                    return BadRequest("New data type is required");

                if (!SqlHelper.IsValidColumnName(request.NewColumnName))
                    return BadRequest("Invalid new column name");

                if (!SqlHelper.IsValidDataType(request.NewDataType))
                    return BadRequest("Invalid data type");

                // Önce veri tipini güncelle
                await _dynamicTableService.UpdateColumn(request);

                return Ok(new { success = true, message = "Column updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating column: {ex.Message}");
            }
        }

        // Sütun silme
        [HttpDelete]
        public async Task<IActionResult> DeleteColumn(string tableName, string columnName)
        {
            try
            {
               await _dynamicTableService.DeleteColumn(tableName, columnName);
                TempData["SuccessMessage"] = "Column deleted successfully";
                return RedirectToAction(nameof(Details), new { tableName });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting column: {ex.Message}";
                return RedirectToAction(nameof(Details), new { tableName });
            }
        }

        // Tablo oluşturma view'ı
        public IActionResult Create()
        {
            return View();
        }

        // Tablo oluşturma işlemi
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTableRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.TableName) || !SqlHelper.IsValidColumnName(request.TableName))
                {
                    return BadRequest("Invalid table name");
                }

                if (request.Columns == null || !request.Columns.Any())
                {
                    return BadRequest("At least one column is required");
                }
               await _dynamicTableService.Create(request);

                TempData["SuccessMessage"] = $"Table {request.TableName} created successfully";
                return Ok(new { success = true, tableName = request.TableName });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating table: {ex.Message}");
            }
        }

        // Tablo silme işlemi
        [HttpDelete]
        public async Task<IActionResult> DeleteTable(string tableName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(tableName) || !SqlHelper.IsValidColumnName(tableName))
                {
                    return BadRequest("Invalid table name");
                }

              await _dynamicTableService.DeleteTable(tableName);

                TempData["SuccessMessage"] = $"Table {tableName} deleted successfully";
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting table: {ex.Message}");
            }
        }

        public IActionResult AddColumn(string tableName)
        {
            ViewBag.TableName = tableName;
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SaveColumn(ColumnModel model)
        {
            if (ModelState.IsValid)
            {
                await _dynamicTableService.AddColumn(model);
                ViewBag.TableName = model.TableName;
                ViewBag.Success = "Ekleme İşlemi Başarılı";

                return RedirectToAction("Details", new { tableName = model.TableName });

            }

            // Model geçersizse tekrar formu göster
            return View("AddColumn", model);
        }
    }
}
