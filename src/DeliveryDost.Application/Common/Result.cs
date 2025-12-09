namespace DeliveryDost.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool isSuccess, T? data, string? errorMessage, string? errorCode)
    {
        IsSuccess = isSuccess;
        Data = data;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, null, null);
    }

    public static Result<T> Failure(string errorMessage, string? errorCode = null)
    {
        return new Result<T>(false, default, errorMessage, errorCode);
    }
}

public class Result
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    private Result(bool isSuccess, string? errorMessage, string? errorCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result Success()
    {
        return new Result(true, null, null);
    }

    public static Result Failure(string errorMessage, string? errorCode = null)
    {
        return new Result(false, errorMessage, errorCode);
    }
}
