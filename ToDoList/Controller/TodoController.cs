using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoangRESTFul.IServices;
using HoangRESTFul.DTO;
using System.Security.Claims;
using HoangRESTFul.Data;

namespace HoangRESTFul.Data
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TodoController : ControllerBase
    {
        private readonly ITodoService _todoService;
        public TodoController(ITodoService todoService)
        {
            _todoService = todoService;
        }
        private Guid getUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                throw new Exception("User ID claim not found");
            }
            return Guid.Parse(userIdClaim.Value);

        }
        [HttpGet("usertodo")]
        public async Task<IActionResult> GetUserTodos(
            [FromQuery] bool? isCompleted,
            [FromQuery] PriorityLevel? priority,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? keyword,
            [FromQuery] DateTime? dueFrom,
            [FromQuery] DateTime? dueTo,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = getUserId();
            var query = new ToDoDto.TodoQueryDto
            {
                IsCompleted = isCompleted,
                Priority = priority,
                CategoryId = categoryId,
                Keyword = keyword,
                DueFrom = dueFrom,
                DueTo = dueTo,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Page = page,
                PageSize = pageSize
            };

            var todos = await _todoService.GetUserTodosAsync(userId, query);
            return Ok(todos);
        }

        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetTodoDetail([FromRoute] Guid id)
        {
            var userId = getUserId();
            var todo = await _todoService.GetTodoByIdAsync(id, userId);
            if (todo == null)
            {
                return NotFound(new { message = "Todo not found" });
            }

            return Ok(todo);
        }

        [HttpPost("addtodo")]
        public async Task<IActionResult> AddTodos([FromBody] ToDoDto.ToDoCreateDto request)
        {
            try
            {
                var userId = getUserId();

                var newTodo = await _todoService.AddCreateTodosAsync(userId, request);
                if (newTodo == null)
                {
                    return BadRequest("Failed to create todo item");
                }
                return Ok(newTodo);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("updatetodo/{id}")]
        public async Task<IActionResult> UpdateTodo([FromRoute] Guid id, [FromBody] ToDoDto.ToDoUpdateDto request)
        {
            try
            {
                var userId = getUserId();
                var updated = await _todoService.UpdateTodoAsync(id, userId, request);
                if (updated == null)
                {
                    return NotFound(new { message = "Todo not found or user unauthorized" });
                }

                return Ok(updated);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("updatestatus/{id}")]
        public async Task<IActionResult> UpdateTodos([FromRoute] Guid id)
        {
            var userId = getUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized("User ID not found in token");
            }
            var result = await _todoService.UpdateStatusAsync(id, userId);
            if (!result) return NotFound(new { message = "Không tìm thấy công việc!" });
            return Ok("Todo item status updated successfully");
        }
        [HttpDelete("deletetodo/{id}")]
        public async Task<IActionResult> DeleteTodos([FromRoute] Guid id)
        {
            var userId = getUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized("User ID not found in token");
            }
            var result = await _todoService.RemoveTodoAsync(id, userId);
            if (!result)
            {
                return NotFound("Todo item not found or user unauthorized");
            }
            return Ok(new {message= "Todo item deleted successfully" });

        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var userId = getUserId();
            var stats = await _todoService.GetTodoStatsAsync(userId);
            return Ok(stats);
        }
    }
}
