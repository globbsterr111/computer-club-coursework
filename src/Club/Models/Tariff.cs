using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Club.Models;

[Table("tariffs", Schema = "dbo")]
public sealed class Tariff
{
    [Key, Column("tariff_id")]
    public int Id { get; set; }

    [Column("tariff_name"), MaxLength(60)]
    public required string Name { get; set; }

    [Column("price_per_hour", TypeName = "decimal(10,2)")]
    public decimal PricePerHour { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;
}
