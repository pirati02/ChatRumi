namespace ChatRumi.Kernel.Results;

/// <summary>
/// Either the successful <typeparamref name="TResponse"/> value or the errors that prevented it.
/// Consume with pattern matching: <c>result switch { TResponse v => ..., IEnumerable&lt;ResponseError&gt; errors => ... }</c>.
/// </summary>
public readonly union Response<TResponse>(TResponse, IEnumerable<ResponseError>)
{
    public static implicit operator Response<TResponse>(ResponseError error) => new([error]);
}
