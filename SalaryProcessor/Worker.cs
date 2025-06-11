using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalaryProcessor.Data;
using SalaryProcessor.Models;
using Microsoft.EntityFrameworkCore;

public class Worker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "rabbitmq" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "salary_payments",
                            durable: true,
                            exclusive: false,
                            autoDelete: false,
                            arguments: null);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=postgres;Database=salarydb;Username=postgres;Password=YourStrong@Passw0rd")
            .Options;

        using var dbContext = new ApplicationDbContext(options);
        dbContext.Database.EnsureCreated();

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Received message: {message}");

            try
            {
                var payment = JsonSerializer.Deserialize<SalaryPayment>(message);

                // แปลง PaymentDate เป็น UTC
                if (payment.PaymentDate.Kind == DateTimeKind.Local)
                    payment.PaymentDate = payment.PaymentDate.ToUniversalTime();
                else if (payment.PaymentDate.Kind == DateTimeKind.Unspecified)
                    payment.PaymentDate = DateTime.SpecifyKind(payment.PaymentDate, DateTimeKind.Utc);

                // ถ้ามี CreatedAt ใน model และต้อง set เอง ให้แปลงเป็น UTC เช่นกัน
                if (payment.GetType().GetProperty("CreatedAt") != null)
                {
                    var createdAt = (DateTime)payment.GetType().GetProperty("CreatedAt").GetValue(payment);
                    if (createdAt.Kind == DateTimeKind.Local)
                        payment.GetType().GetProperty("CreatedAt").SetValue(payment, createdAt.ToUniversalTime());
                    else if (createdAt.Kind == DateTimeKind.Unspecified)
                        payment.GetType().GetProperty("CreatedAt").SetValue(payment, DateTime.SpecifyKind(createdAt, DateTimeKind.Utc));
                }

                dbContext.SalaryPayments.Add(payment);
                dbContext.SaveChanges();
                Console.WriteLine($"Successfully saved payment for employee {payment.EmployeeName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        };

        channel.BasicConsume(queue: "salary_payments",
                            autoAck: true,
                            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
} 