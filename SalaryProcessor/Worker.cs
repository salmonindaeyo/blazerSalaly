using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SalaryProcessor.Data;
using SalaryProcessor.Models;
using Microsoft.EntityFrameworkCore;
using iCash.Services;
using Microsoft.Extensions.Configuration;

public class Worker : BackgroundService
{
    private readonly EncryptionService _encryptionService;

    public Worker(IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var encryptionSettings = configuration.GetSection("Encryption");
        _encryptionService = new EncryptionService(
            encryptionSettings["Key"],
            null // IV is no longer needed
        );
    }

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

        using (var checkDbContext = new ApplicationDbContext(options))
        {
            checkDbContext.Database.EnsureCreated();
        }

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var encryptedMessage = Encoding.UTF8.GetString(body);
                Console.WriteLine(encryptedMessage);
                
                byte[] decodedBytes = Convert.FromBase64String(encryptedMessage);
                string decodedText = Encoding.UTF8.GetString(decodedBytes);
                
                var message = _encryptionService.Decrypt(decodedText);
                Console.WriteLine("--------------------------------");
                Console.WriteLine(message);
                var payment = JsonSerializer.Deserialize<SalaryPayment>(message);

                payment.PaymentDate = payment.PaymentDate.ToUniversalTime();
                payment.CreatedAt = DateTime.UtcNow; // กำหนดค่า CreatedAt เป็นเวลาปัจจุบันใน UTC

                using (var dbContext = new ApplicationDbContext(options))
                {
                    // ตรวจสอบว่ามีข้อมูลนี้อยู่แล้วหรือไม่
                    var existingPayment = dbContext.SalaryPayments
                        .FirstOrDefault(p => p.TransactionId == payment.TransactionId);
                        
                    if (existingPayment != null)
                    {
                        Console.WriteLine($"ข้อมูลการจ่ายเงินเดือนสำหรับ TransactionId {payment.TransactionId} มีอยู่แล้ว ไม่บันทึกซ้ำ");
                    }
                    else
                    {
                        dbContext.SalaryPayments.Add(payment);
                        dbContext.SaveChanges();
                        Console.WriteLine($"บันทึกข้อมูลการจ่ายเงินเดือนสำหรับ {payment.EmployeeName} เรียบร้อยแล้ว");
                    }
                }

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"เกิดข้อผิดพลาด: {ex.Message}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"รายละเอียดข้อผิดพลาด: {ex.InnerException.Message}");
                }
                
                if (ex.InnerException != null && (ex.InnerException.Message.Contains("duplicate key") || 
                                                ex.InnerException.Message.Contains("unique constraint")))
                {
                    Console.WriteLine("ข้อมูลซ้ำ จึงไม่บันทึกข้อมูลนี้");
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    // หากเป็นข้อผิดพลาดอื่น ให้ส่งข้อความกลับคิวเพื่อลองใหม่
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            }
        };

        channel.BasicConsume(queue: "salary_payments",
                            autoAck: false,
                            consumer: consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
} 