using UserService.Models;

namespace UserService.ResponseModels
{
    public class UserGetResponse
    {
        public int Id { get; set; }

        public String FirstName { get; set; }

      
        public String LastName { get; set; }

        public String Username { get; set; }

        
        public string Email { get; set; }

        
        public Role Role{ get; set; }

        public List<UserAddress> UserAddress { get; set; }

        public UserGetResponse(int id,string firstName,string secondName,string userName,string email,Role role,List<UserAddress> userAddress)
        {
            Id = id;
            FirstName = firstName;
            LastName = secondName;
            Username = userName;
            Email = email;
            Role = role;
            UserAddress = userAddress;
        }
    }
}
