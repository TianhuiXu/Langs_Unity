// Copyright 2022 ReWaffle LLC. All rights reserved.

namespace Naninovel
{
    /// <summary>
    /// Implantation is able to convert objects.
    /// </summary>
    public interface IConverter
    {
        object Convert (object obj, string name);

        UniTask<object> ConvertAsync (object obj, string name);
    }

    /// <summary>
    /// Implantation is able to convert <typeparamref name="TSource"/> to <typeparamref name="TResult"/>.
    /// </summary>
    public interface IConverter<TSource, TResult> : IConverter
    {
        TResult Convert (TSource obj, string name);

        UniTask<TResult> ConvertAsync (TSource obj, string name);
    }
}
