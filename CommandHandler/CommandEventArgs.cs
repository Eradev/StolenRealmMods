using System;
using System.Collections.Generic;
using System.Linq;

namespace eradev.stolenrealm.CommandHandlerNS
{
    public class CommandEventArgs : EventArgs
    {
        public string Name;

        public List<string> Args;

        public CommandEventArgs(IReadOnlyList<string> args)
        {
            Name = args[0];
            Args = args.Skip(1).ToList();
        }
    }
}