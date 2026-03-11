using Blast.Core.Results;

namespace OpenHeidiSQLProfiles.Fluent.Plugin
{
    public class OpenProfileSearchOperation : SearchOperationBase
    {
        public static OpenProfileSearchOperation OpenProfileOperation { get; } =
            new OpenProfileSearchOperation();

        public OpenProfileSearchOperation() : base(
            "Open Profile",
            "Opens the HeidiSQL profile.",
            "\ue8a7")
        {
        }
    }
}
