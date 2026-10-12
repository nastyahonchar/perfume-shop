using System.ComponentModel.DataAnnotations;

namespace Orders.Bll.Dtos;

public class ChangeStatusDto
{
    [Required, StringLength(30)]
    public string Status { get; set; } = "";
}