﻿using System.Runtime.Serialization;

namespace PListNet;

/// <summary>
/// PListNet format exception.
/// </summary>
public class PListFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PListFormatException"/> class.
    /// </summary>
    public PListFormatException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PListFormatException"/> class.
    /// </summary>
    /// <param name="message">Message.</param>
    public PListFormatException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PListFormatException"/> class.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="inner">Inner.</param>
    public PListFormatException(string message, Exception inner) : base(message, inner)
    {
    }

    protected PListFormatException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}
