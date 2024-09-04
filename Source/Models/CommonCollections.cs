namespace WatchedFilmsTracker.Source.Models
{
    public static class CommonCollections
    {
        public static Dictionary<string, CommonCollection> CommonCollectionsSet { get; } = new Dictionary<string, CommonCollection>();
        private static List<CommonCollection> _sortedCollections;

        static CommonCollections()
        {
            foreach (CollectionType collectionType in Enum.GetValues(typeof(CollectionType)))
            {
                string collectionTypeName = collectionType.ToString();

                CommonCollection collection = new CommonCollection()
                {
                    Name = collectionTypeName,
                    DefaultColumnHeaders = GetColumnHeaders(collectionType)
                };

                CommonCollectionsSet.Add(collectionTypeName, collection);
            }
        }

        public enum CollectionType
        {
            Books,
            Comics,
            Films,
            Games,
            Other,
            Series,
        }

        public static IEnumerable<CommonCollection> GetAllCollectionsAlphabetically()
        {
            if (_sortedCollections == null)
            {
                _sortedCollections = CommonCollectionsSet
               .OrderBy(kv => kv.Key)
               .Select(kv => kv.Value)
               .ToList();
            }
            return _sortedCollections;
        }

        public static CommonCollection GetCommonCollectionByName(string name)
        {
            if (CommonCollectionsSet.ContainsKey(name))
                return CommonCollectionsSet[name];
            return null;
        }

        public static CommonCollection GetCommonCollectionByName(CollectionType name)
        {
            if (CommonCollectionsSet.ContainsKey(name.ToString()))
                return CommonCollectionsSet[name.ToString()];
            return null;
        }

        private static List<string> GetColumnHeaders(CollectionType type)
        {
            return type switch
            {
                CollectionType.Films => new List<string>
                { "English Title", "Original Title", "Type", "Release Year", "Rating", "Finish Date", "Watched In" , "Comments", "Review",},
                CollectionType.Books => new List<string>
                { "English Title", "Original Title", "Author","Release Year", "Rating", "Finish date",  "Comments" , "Review"},
                CollectionType.Comics => new List<string>
                { "English Title", "Original Title", "Type", "Author", "Release Year", "Rating", "Finish Date", "Comments", "Review" },
                CollectionType.Games => new List<string>
                { "English Title", "Original Title", "Platform", "Release Year", "Rating", "Status", "Finish Date", "Comments", "Review" },
                CollectionType.Series => new List<string>
                { "English Title", "Original Title", "Type", "Release Year", "Rating", "Last Watched", "Status", "Finish Date", "Comments", "Review" },
                CollectionType.Other => new List<string>
                { "Column 1", "Column 2", "Column 3" },
                _ => new List<string>()
            };
        }
    }
}