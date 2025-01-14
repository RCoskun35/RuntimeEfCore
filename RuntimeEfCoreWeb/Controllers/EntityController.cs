using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RuntimeEfCoreWeb.Controllers
{
    [Route("api/{entityName}")]
    [ApiController]
    public class EntityController : ControllerBase
    {
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
            var item = items.FirstOrDefault(i => i.GetType().GetProperty("Id").GetValue(i).ToString() == id);
            return Ok(item);
        }

        [HttpPost]
        public IActionResult Post(string entityName, [FromBody] object item)
        {
            //var items = DynamicContextExtensions.GetEntity(entityName);
            //items.Add(item);
            //return Ok(items);
            return Ok();
        }
        [HttpPut("{id}")]
        public IActionResult Put(string entityName, string id, [FromBody] object item)
        {
            //var items = DynamicContextExtensions.GetEntity(entityName);
            //var index = items.FindIndex(i => i.GetType().GetProperty("Id").GetValue(i).ToString() == id);
            //items[index] = item;
            //return Ok(items);
            return Ok();
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(string entityName, string id)
        {
            //var items = DynamicContextExtensions.GetEntity(entityName);
            //var index = items.FindIndex(i => i.GetType().GetProperty("Id").GetValue(i).ToString() == id);
            //items.RemoveAt(index);
            //return Ok(items);

            return Ok();
        }
    }
}
