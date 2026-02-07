// Copyright (c) Doug Swisher. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Micasa.Cli.Exceptions
{
    public class MicasaException : Exception
    {
        public MicasaException(string message)
            : base(message)
        {
        }


        public MicasaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
