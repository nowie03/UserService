using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Context
{
    public class ServiceContext : DbContext
    {
        public ServiceContext(DbContextOptions dbContextOptions) : base(dbContextOptions) { }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<UserAddress> UsersAddresses { get; set; }

        public DbSet<Message> Outbox { get; set; }

        

    }
}
