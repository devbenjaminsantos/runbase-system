namespace RunBase.Application.Orders;

public enum OrderError
{
    None,
    NotFound,
    ClientNotFound,
    InvalidAmount,
    InvalidStatusTransition
}
