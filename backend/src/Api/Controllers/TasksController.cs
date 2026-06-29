using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskManagementSystem.Api.Contracts;
using TaskManagementSystem.BusinessLogic.Exceptions;
using TaskManagementSystem.BusinessLogic.Tasks;

namespace TaskManagementSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequestDto request)
    {
        try
        {
            var task = await _taskService.CreateAsync(
                CurrentUserId, new CreateTaskRequest(request.Title, request.Description, request.Status, request.DueDate));

            return CreatedAtAction(nameof(GetById), new { id = task.Id }, TaskResponseDto.FromEntity(task));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tasks = await _taskService.GetAllAsync(CurrentUserId);
        return Ok(tasks.Select(TaskResponseDto.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var task = await _taskService.GetByIdAsync(CurrentUserId, id);
            return Ok(TaskResponseDto.FromEntity(task));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTaskRequestDto request)
    {
        try
        {
            var task = await _taskService.UpdateAsync(
                CurrentUserId, id, new UpdateTaskRequest(request.Title, request.Description, request.Status, request.DueDate));

            return Ok(TaskResponseDto.FromEntity(task));
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
        catch (ValidationException ex)
        {
            return BadRequest(new ErrorResponseDto(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _taskService.DeleteAsync(CurrentUserId, id);
            return NoContent();
        }
        catch (NotFoundException ex)
        {
            return NotFound(new ErrorResponseDto(ex.Message));
        }
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
