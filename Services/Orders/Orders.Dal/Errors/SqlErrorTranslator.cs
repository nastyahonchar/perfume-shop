using Microsoft.Data.SqlClient;
using Orders.Domain.Exceptions;

namespace Orders.Dal.Errors;

public static class SqlErrorTranslator
{
    public static Exception? Translate(SqlException ex) => ex.Number switch
    {
        50001 or 50004 or 50006 => new NotFoundException(ex.Message),
        50002 or 50003 or 50005 or 50007 or 50009 => new BusinessConflictException(ex.Message),
        50008 => new ValidationException(ex.Message),
        2601 or 2627 => new BusinessConflictException("Record with the same key already exists"),
        547 => new BusinessConflictException("The operation violates a data constraint"),
        _ => null
    };
}