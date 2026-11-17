using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("payment_methods", Schema = "dbo")]
public sealed class PaymentMethod
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.None), Column("payment_method_id")]
    public int Id { get; set; }

    [Column("method_name"), MaxLength(30)]
    public required string Name { get; set; }
}
