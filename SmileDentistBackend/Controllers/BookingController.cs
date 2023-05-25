using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmileDentistBackend.Data;
using SmileDentistBackend.Utility;
using System.Data;

namespace SmileDentistBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly QuartzContext _context;

        public BookingController(QuartzContext context)
        {
            _context = context;
        }
        [Authorize(Roles = StaticDetails.Role_Admin)]
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] DateTime? fields, [FromQuery] DateTime? endValue, [FromQuery] bool orderedName = true, [FromQuery] bool orderedEmail = true)
        {
            if (fields != null && endValue != null)
            {
                var sortedFields = await _context.Bookings.Where(x => x.ScheduledTime.Value >= fields.Value.Date && x.ScheduledTime.Value.Date <= endValue.Value.Date.AddHours(23).AddMinutes(59)).ToListAsync();
                var specificField = sortedFields.OrderBy(x => x.Email);

                if (sortedFields != null)
                {
                    return Ok(sortedFields);
                }
            }
            var bookings = await _context.Bookings.ToListAsync();
            if (bookings != null)
            {
                return Ok(bookings);
            }
            return NotFound("The database yielded no results");
        }

        [Authorize(Roles = StaticDetails.Role_Admin)]
        [HttpGet("Users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userList = await _context.ApplicationUsers.Select(x => new { x.UserName, x.Id, x.Name }).ToListAsync();
            if (userList.Count > 0)
            {
                return Ok(userList);
            }
            return NotFound("No user exists in the database");
        }

        [Authorize(Roles = StaticDetails.Role_User)]
        [HttpGet("Users/{id}")]
        [ResponseCache(Duration = 60)]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var currentCustomerList = await _context.Bookings.Where(x => x.UserId == id).ToListAsync();
            if (currentCustomerList != null)
            {
                return Ok(currentCustomerList);
            }
            return NotFound("No times was found for the current user");
        }



        [Authorize(Roles = StaticDetails.Role_Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBooking(int id)
        {
            var bookedItem = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id);
            if (bookedItem != null)
            {
                _context.Bookings.Remove(bookedItem);
                await _context.SaveChangesAsync();
                return Ok($"Item {id} was removed from the database");
            }
            return NotFound($"No object with id: {id} exists in the database");
        }

        [Authorize(Roles = StaticDetails.Role_User)]
        [HttpDelete("userdelete/{id}")]
        public async Task<IActionResult> DeleteUserBooking(int id)
        {
            var bookedItem = await _context.Bookings.FirstOrDefaultAsync(x => x.Id == id);
            if (bookedItem != null)
            {
                _context.Bookings.Remove(bookedItem);
                await _context.SaveChangesAsync();
                return Ok($"Item {id} was removed from the database");
            }
            return NotFound($"No object with id: {id} exists in the database");
        }
    }
}
