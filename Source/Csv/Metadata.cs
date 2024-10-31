using System.Text;

namespace WatchedFilmsTracker.Source.Csv
{
    public class Metadata
    {
        public string AllComment { get; set; }
        public string Comment { get; set; }
        public string CommentAfter { get; set; }
        public string CommentBefore { get; set; }
        public int CommentLines { get; set; }
        public string ProgramVersion { get; set; }

        public Dictionary<string, string> Values { get; set; }

        private StringBuilder _comment;
        private StringBuilder _commentAfter;
        private StringBuilder _commentBefore;

        public Metadata(List<string> commentLines)
        {
            _commentBefore = new StringBuilder();
            _commentAfter = new StringBuilder();
            _comment = new StringBuilder();

            CommentBefore = "";
            Comment = "";
            CommentAfter = "";
            ProcessLines(commentLines);
        }

        private void ProcessLines(List<string> commentLines)
        {
            if (commentLines != null)
            {
                bool programCommentRead = false;
                foreach (string commentLine in commentLines)
                {
                    if (commentLine.StartsWith("# {\"WatchedFilmsTracker\":") && !programCommentRead)
                    {
                        _comment.AppendLine(commentLine);
                        programCommentRead = true;
                    }
                    else if (!programCommentRead)
                    {
                        _commentBefore.AppendLine(commentLine);
                    }
                    else
                    {
                        _commentAfter.AppendLine(commentLine);
                    }
                }

                Comment = _comment.ToString().Trim();
                CommentBefore = _commentBefore.ToString().Trim();
                CommentAfter = _commentAfter.ToString().Trim();
            }
        }
    }
}