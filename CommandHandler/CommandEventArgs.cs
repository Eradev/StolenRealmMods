using System;
using System.Collections.Generic;
using System.Linq;

namespace eradev.stolenrealm.CommandHandlerNS
{
    public class CommandEventArgs : EventArgs
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public string Name;

        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        public List<string> Args;

        public CommandEventArgs(IReadOnlyList<string> args)
        {
            Name = args[0];
            Args = args.Skip(1).ToList();
        }
    }
}