namespace InventoryManagement.Application.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message = "The record was modified by another user. Refresh and try again.")
        : base(message) { }
}
