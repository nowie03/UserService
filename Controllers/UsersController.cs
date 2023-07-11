using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using UserService.Constants;
using UserService.Context;
using UserService.MessageBroker;
using UserService.Models;
using UserService.ResponseModels;

namespace UserService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ServiceContext _context;

        private readonly IMessageBrokerClient _rabbitMQClient;

        public UsersController(ServiceContext context, IServiceProvider serviceProvider)
        {
            _context = context;
            _rabbitMQClient = serviceProvider.GetRequiredService<IMessageBrokerClient>();
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserGetResponse>>> GetUsers(int limit, int skip)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            List<User> users = await _context.Users
                .Skip(skip)
                .Take(limit)
                .AsNoTracking()
                .ToListAsync();

            List<UserGetResponse> responses = new();
            try
            {
                foreach (var user in users)
                {
                    Role? roleOfUser = await _context.Roles.FindAsync(user.RoleId) ?? throw new Exception($"Cannot find a role for user {user.Id}");

                    IEnumerable<UserAddress>? addressesOfUser = _context.UsersAddresses.Where(ua => ua.UserId == user.Id) ?? throw new Exception($"cannot find address for ${user.Id}");

                    responses.Add(new UserGetResponse(user.Id, user.FirstName, user.LastName, user.Username, user.Email, roleOfUser, addressesOfUser));


                }
                return Ok(responses);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserGetResponse>> GetUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            try
            {

                Role? roleOfUser = await _context.Roles.FindAsync(user.RoleId) ?? throw new Exception($"Cannot find a role for user {user.Id}");

                List<UserAddress>? addressesOfUser = _context.UsersAddresses.Where(ua => ua.UserId == user.Id).ToList() ?? throw new Exception($"cannot find address for ${user.Id}");

                return (new UserGetResponse(user.Id, user.FirstName, user.LastName, user.Username, user.Email, roleOfUser, addressesOfUser));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (_context.Users == null)
            {
                return Problem("Entity set 'ServiceContext.Users'  is null.");
            }
            using var transaction = _context.Database.BeginTransaction();
            try
            {

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                //add this user added event to outbox 
                ulong nextSequenceNumber = _rabbitMQClient.GetNextSequenceNumer();
                string serializedUser = JsonConvert.SerializeObject(user);

                Message outobxMessage = new Message(EventTypes.USER_CREATED, serializedUser, nextSequenceNumber, EventStates.EVENT_ACK_PENDING);

                await _context.Outbox.AddAsync(outobxMessage);
                await _context.SaveChangesAsync();



                transaction.Commit();

                return CreatedAtAction("GetUser", new { id = user.Id }, user);
            }
            catch (DbUpdateException ex)
            {
                transaction.Rollback();
                return BadRequest("User already exixts with that credentials");
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException)
            {
                transaction.Rollback();
                return Problem("unable to reach message queue try again");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Problem("something went wrong try again");
            }
        }

        [HttpGet]
        [Route("address")]
        public async Task<ActionResult<IEnumerable<UserAddress>>>GetUserAddress(int userId)
        {
            if (_context.UsersAddresses == null)
                return NoContent();
            try
            {
                var userAdress = await _context.UsersAddresses.Where(address => address.UserId == userId).ToListAsync();
                return Ok(userAdress);

            }catch(Exception ex)
            {
                return BadRequest();
            }
            
        }

        [HttpPost]
        [Route("address")]
        public async Task<ActionResult<UserAddress>> PostUserAddress(UserAddress userAddress)
        {
            User? user = await _context.Users.FindAsync(userAddress.UserId);

            if (user == null)
                return BadRequest();
            try
            {
                await _context.UsersAddresses.AddAsync(userAddress);
                await _context.SaveChangesAsync();

                return Ok(userAddress);

            }
            catch (Exception ex)
            {
                return Problem(ex.Message);
            }
        }

        [HttpDelete]
        [Route("address")]
        public async Task<IActionResult> DeleteUserAddress(int id)
        {
            if (_context.UsersAddresses == null)
            {
                return NotFound();
            }
            var address = await _context.UsersAddresses.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }

            _context.UsersAddresses.Remove(address);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (_context.Users == null)
            {
                return NotFound();
            }
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            using var transaction = _context.Database.BeginTransaction();

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                ulong nextSequenceNumber = _rabbitMQClient.GetNextSequenceNumer();
                string serializedUser = JsonConvert.SerializeObject(user);

                Message outobxMessage = new Message(EventTypes.USER_DELETED, serializedUser, nextSequenceNumber, EventStates.EVENT_ACK_PENDING);

                await _context.Outbox.AddAsync(outobxMessage);
                await _context.SaveChangesAsync();


                transaction.Commit();
                return NoContent();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Problem(ex.Message);
            }
        }

        private bool UserExists(int id)
        {
            return (_context.Users?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
