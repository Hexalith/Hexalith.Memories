// <copyright file="InsecureTokenTransportExceptionTests.cs" company="ITANEO">
// Copyright (c) ITANEO (https://www.itaneo.com). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace Hexalith.Memories.Cli.Tests.Cli;

using Hexalith.Memories.Cli.Configuration;

using Shouldly;

public class InsecureTokenTransportExceptionTests
{
    [Theory]
    [InlineData("http://memories.example.com/", "t", true)]      // http + non-localhost + token → refuse
    [InlineData("http://127.0.0.1:5000/", "t", false)]           // localhost exception
    [InlineData("http://localhost:5000/", "t", false)]           // localhost exception (named)
    [InlineData("https://memories.example.com/", "t", false)]    // https ok
    [InlineData("http://memories.example.com/", null, false)]    // no token → safe
    [InlineData("http://memories.example.com/", "", false)]      // empty token → safe
    public void ShouldRefuse_EvaluatesPlaintextTokenGuardCorrectly(string endpoint, string? token, bool expected)
    {
        bool result = InsecureTokenTransportException.ShouldRefuse(new Uri(endpoint), token);
        result.ShouldBe(expected);
    }
}
