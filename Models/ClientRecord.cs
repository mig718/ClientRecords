using System.ComponentModel.DataAnnotations;

namespace ClientRecords.Models;

public class ClientRecord
{
    public int ClientId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string TaxId { get; set; } = string.Empty;

    [Required]
    [StringLength(2, MinimumLength = 2)]
    public string CountryCode { get; set; } = string.Empty;
}
