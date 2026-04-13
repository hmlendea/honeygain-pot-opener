using NuciLog.Core;

namespace HoneygainPotOpener.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name) : base(name) { }

        public static Operation LogIn => new MyOperation(nameof(LogIn));

        public static Operation OpenPot => new MyOperation(nameof(OpenPot));
    }
}