using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("payments", Schema = "dbo")]
public sealed class Payment
{
    [Key, Column("payment_id")]
    public int Id { get; set; }

    [Column("session_id")]
    public int SessionId { get; set; }

    [Column("amount", TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Column("payment_time", TypeName = "datetime2(0)")]
    public DateTime PaymentTime { get; set; }

    [Column("payment_method_id")]
    public int PaymentMethodId { get; set; }
}
