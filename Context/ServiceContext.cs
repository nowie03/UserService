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

        override
        protected void OnModelCreating(ModelBuilder builder)
        {
            //unique seq numbers to messages to prevent message duping
            builder.Entity<Message>().HasIndex(message => message.SequenceNumber).IsUnique();
        }


    }
}
