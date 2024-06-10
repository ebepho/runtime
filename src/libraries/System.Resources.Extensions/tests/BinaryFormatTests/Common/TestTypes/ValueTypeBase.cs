﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

public interface ValueTypeBase : IDeserializationCallback
{
    public string Name { get; set; }

    public BinaryTreeNodeWithEventsBase? Reference { get; set; }
}
