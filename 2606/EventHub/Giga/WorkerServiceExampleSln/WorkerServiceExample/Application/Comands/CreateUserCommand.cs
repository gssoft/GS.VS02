// Application/Comands/CreateUserCommand.cs

using Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Application.Commands;
// DTO (Data Transfer Object) для команды. Сама команда не должна содержать логику.
public class CreateUserCommand : ICommand
{
    [Required]
    public string Username { get; set; } = string.Empty;
}
