﻿//HintName: IAsyncOverloadInterface.g.cs
// <auto-generated/>
using System;
using System.Net;
using Lombok.NET;

namespace Test;
#nullable enable
internal partial interface IAsyncOverloadInterface
{
    global::System.Threading.Tasks.Task RunAsync(int i, global::System.Threading.CancellationToken cancellationToken = default);
    global::System.Threading.Tasks.Task<bool> IsValidAsync(HttpStatusCode statusCode, global::System.Threading.CancellationToken cancellationToken = default);
}