using Microsoft.EntityFrameworkCore;
using UserService.BackgroundServices;
using UserService.Context;
using UserService.MessageBroker;

namespace UserService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<ServiceContext>(options => options.UseSqlServer(
                builder.Configuration.GetConnectionString("local-server")));

            //add rabbitmqclient as scoped service
            builder.Services.AddScoped<IMessageBrokerClient, RabbitMQClient>();

            //add background service
            builder.Services.AddSingleton<PublishMessageToQueue>();
            builder.Services.AddHostedService<PublishMessageToQueue>(
                provider => provider.GetRequiredService<PublishMessageToQueue>());

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI();
            

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}