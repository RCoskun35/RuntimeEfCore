using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Newtonsoft.Json;
using System.Linq;

namespace RuntimeEfCoreWeb.Controllers
{
    [Route("api/{entityName}")]
    [ApiController]
    [EnableQuery]
    public class EntityController : ControllerBase
    {
        public EntityController(IConfiguration configuration)
        {
            DynamicContextExtensions.DynamicContext(configuration.GetConnectionString("DefaultConnection")!);
        }
        [HttpGet]
        public IActionResult Get(string entityName)
        {
            var items = DynamicContextExtensions.GetEntity(entityName);
            return Ok(items);
        }
        [HttpGet("{id}")]
        public IActionResult GetById(string entityName, string id)
        {
            var items = DynamicContextExtensions.GetEntity(entityName);
            var item = items.Where(i => i.GetType().GetProperty("Id").GetValue(i).ToString() == id.ToLower()).AsEnumerable().FirstOrDefault();
            return Ok(item);
        }
        [HttpPost]
        public IActionResult Post(string entityName, [FromBody] object item)
        {
            var entityType = DynamicContextExtensions.dynamicContext.Model.FindEntityType($"TypedDataContext.Models.{entityName}")?.ClrType;

            if (entityType == null)
            {
                return StatusCode(StatusCodes.Status404NotFound, $"Entity type: {entityName} not found");
            }

            // Deserialize the incoming item into a dynamic object
            var deserializedItem = JsonConvert.DeserializeObject(item.ToString(), entityType);

            if (deserializedItem == null)
            {
                return BadRequest("Invalid object format.");
            }
            Guid newId;
            // Check if the entity type has an Id property and set it if necessary
            var idProperty = entityType.GetProperty("Id");
            if (idProperty != null)
            {
                // Generate a new Id (for example, using GUID)
                 newId = Guid.NewGuid(); // or any other logic for generating Id
                idProperty.SetValue(deserializedItem, newId);
            }
            else
            {
                return BadRequest("The entity type does not have an Id property.");
            }

            DynamicContextExtensions.dynamicContext.Add(deserializedItem);
            DynamicContextExtensions.dynamicContext.SaveChanges();
            return CreatedAtAction(nameof(GetById), new { entityName, id = newId }, deserializedItem);
        }


        [HttpPut("{id}")]
        public IActionResult Put(string entityName, string id, [FromBody] object item)
        {
            try
            {
                // Get the entity type
                var entityType = DynamicContextExtensions.dynamicContext.Model
                        .FindEntityType($"TypedDataContext.Models.{entityName}")?.ClrType;
                if (entityType == null)
                {
                    return NotFound($"Entity type '{entityName}' not found.");
                }

                // Get the DbSet for the entity type
                var dbSet = DynamicContextExtensions.dynamicContext.Query($"TypedDataContext.Models.{entityName}", entityType);

                // Find the item by Id
                var keyProperty = entityType.GetProperties().FirstOrDefault(p => p.Name == "Id");
                if (keyProperty == null)
                {
                    return BadRequest("No 'Id' property found on the entity.");
                }

                // Use AsEnumerable to switch to client-side evaluation
                var existingItems = dbSet.Cast<object>().ToList();
                var existingItem = existingItems.FirstOrDefault(e =>
                    keyProperty.GetValue(e)?.ToString() == id);

                if (existingItem == null)
                {
                    return NotFound($"Item with Id '{id}' not found.");
                }

                // Deserialize the incoming item to the correct entity type
                var updatedItem = JsonConvert.DeserializeObject(item.ToString(), entityType);
                if (updatedItem == null)
                {
                    return BadRequest("Invalid object format.");
                }

                // Update the properties of the existing item
                foreach (var property in entityType.GetProperties())
                {
                    if (property.Name != "Id")
                    {
                        var newValue = property.GetValue(updatedItem);
                        property.SetValue(existingItem, newValue);
                    }
                }

                // Save changes to the database
                DynamicContextExtensions.dynamicContext.Update(existingItem);
                DynamicContextExtensions.dynamicContext.SaveChanges();

                return Ok(existingItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(string entityName, string id)
        {
            try
            {
                // Get the entity type
                var entityType = DynamicContextExtensions.dynamicContext.Model
                        .FindEntityType($"TypedDataContext.Models.{entityName}")?.ClrType;
                if (entityType == null)
                {
                    return NotFound($"Entity type '{entityName}' not found.");
                }

                // Get the DbSet for the specified entity
                var dbSet = (IQueryable<object>)DynamicContextExtensions.dynamicContext.Query($"TypedDataContext.Models.{entityName}", entityType);

                // Find the item to delete
                var keyProperty = entityType.GetProperties().FirstOrDefault(p => p.Name == "Id");
                if (keyProperty == null)
                {
                    return BadRequest("Entity does not have an 'Id' property.");
                }

                var existingItem = dbSet.AsEnumerable().FirstOrDefault(e =>
                    keyProperty.GetValue(e)?.ToString() == id);

                if (existingItem == null)
                {
                    return NotFound($"Item with Id '{id}' not found.");
                }

                // Remove the item from the context
                DynamicContextExtensions.dynamicContext.Remove(existingItem);

                // Save changes to the database
                DynamicContextExtensions.dynamicContext.SaveChanges();

                return Ok($"Item with Id '{id}' successfully deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

    }
}
