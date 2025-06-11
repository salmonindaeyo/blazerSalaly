using System;

namespace SalaryProcessor.Models
{
    public class SalaryPayment
    {
        public int Id { get; set; }
        public string EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public decimal Salary { get; set; }
        public DateTime PaymentDate { get; set; }
        public string TransactionId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
} 