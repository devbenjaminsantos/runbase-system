namespace RunBase.Application.Plans;

public sealed class PlanResult<T>
{
    private PlanResult(T? value, PlanError error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public PlanError Error { get; }

    public bool Succeeded => Error == PlanError.None;

    public static PlanResult<T> Success(T value)
    {
        return new PlanResult<T>(value, PlanError.None);
    }

    public static PlanResult<T> Failure(PlanError error)
    {
        return new PlanResult<T>(default, error);
    }
}
