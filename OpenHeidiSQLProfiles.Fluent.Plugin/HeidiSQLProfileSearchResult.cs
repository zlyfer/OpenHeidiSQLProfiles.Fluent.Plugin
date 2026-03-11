using Blast.Core.Interfaces;
using Blast.Core.Results;
using System.Collections.Generic;

namespace OpenHeidiSQLProfiles.Fluent.Plugin
{
    public sealed class HeidiSQLProfileSearchResult : SearchResultBase
    {
        public HeidiSQLProfileSearchResult(
            string searchAppName,
            string profileName,
            string searchedText,
            double score,
            IList<ISearchOperation> supportedOperations,
            ICollection<SearchTag> tags) : base(
            searchAppName,
            profileName,
            searchedText,
            "HeidiSQL Profile",
            score,
            supportedOperations,
            tags)
        {
            ProfileName = profileName;

            MlFeatures = new Dictionary<string, string>
            {
                ["ProfileName"] = profileName
            };
        }

        public string ProfileName { get; }

        protected override void OnSelectedSearchResultChanged()
        {
        }
    }
}
